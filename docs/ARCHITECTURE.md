# Architecture

## Where it sits

```
Reflect XR headset ─┐                          ┌─ Reflect.Web (MVC)  ← Provider Area lives here
Reflect Mobile ─────┼─ Firebase Analytics ─ BigQuery export ─┐       │
                    └─ Reflect.Api (REST) ─ Azure SQL ───────┴── repositories (Reflect.Infrastructure)
```

The dashboard keeps **product analytics** and **clinical data** apart, as the use case asks:

| Kind | Entities | Production source | Demo |
|---|---|---|---|
| Product analytics | `AppSession` | Firebase Analytics events (`session_start`, `module_open`, `module_complete`, `session_end`) exported to BigQuery, read through the secure API | In-memory |
| Clinical | `ProviderClient`, `MoodCheckIn`, `AssessmentResult`, `Observation` | Azure SQL (`clinical` schema), written by Reflect.Api | In-memory |
| Billing / documentation | `RtmTimeEntry`, `ProviderProfile` | Azure SQL, written by the web app (audit-logged) | In-memory |

Analytics never carries clinical content: sessions join to clients only by `ClientUserId` inside the backend.
No PHI goes to Firebase.

## Layers (matches the spec's Clean Architecture)

- **Reflect.Domain**: entities, repository interfaces, and all rules as pure static classes
  (`ProviderDashboard/Rtm`, `ProviderDashboard/Clinical`). No framework dependencies.
- **Reflect.Web**: thin controllers → fat ViewModels (`PrepareData` / `PrepareDataForSave`) → Razor views →
  per-screen JS (`wwwroot/js/controllers/provider/*`, `wwwroot/js/services/provider/*`).

## Screens load in pieces

The dashboard and client page render a light shell (header, KPIs or RTM status strip) and empty tab panes.
`wwwroot/js/shared/provider/providerTabs.js` fetches a tab's partial view the first time it is opened (one request
per tab, remembered across reloads), initializes its tables, and builds each chart only when its canvas is visible.
Each tab has its own ViewModel that loads only the data it shows (for example, the Physiologic tab reads only 30 days
of observations). Inside tabs, `_ViewToggle` switches between alternative views of the same data (charts vs. table,
mood vs. stress, by day vs. by app) without another request.

## Live data

```
Reflect XR / Mobile ──POST /api/ingest/*──┐
Live demo page / auto-simulator ──────────┴─> IngestionService ─> repositories + IngestEvent
                                                                      │
open Provider pages ── poll /Provider/Live/Feed every 5 s ────────────┘ ─> toast, refresh KPIs / status strip / open tab
```

- One code path: the API and the simulator both go through `IngestionService` (validation, mapping, event record).
- The API is machine-to-machine: `X-Reflect-Ingest-Key` header compared in constant time; no cookies, no anti-forgery.
  Payloads are validated (known client, module belongs to platform, scales 1-10, PHQ-9 0-27, item 9 0-3, no future
  times, sessions under 4 hours, emotion words sanitized).
- The feed returns only events for the signed-in provider's clients, never more than 10 minutes back, and pauses while
  the browser tab is hidden. Urgent events (PHQ-9 item 9) raise a 12-second error toast on every open page.
- Production: Reflect.Api (or a scheduled BigQuery export job) calls the same endpoints; the feed can move to the
  real app's SignalR hub (CSP already allows `wss:`).

## Clinical assistant (AI)

- Server-side only: the browser posts the question to `/Provider/Assistant/Ask`; the server calls the AI. The API key is
  read from user-secrets / environment and never reaches the browser. The CSP still blocks every external host.
- **De-identified grounding** (`ClinicalContextBuilder`): the model receives a compact text summary of the client's (or
  caseload's) data with tokens instead of names (`[CLIENT]`, `[C03]`), 10-year age bands instead of birthdates, relative
  days instead of dates, and no MRN, payer or NPI. Prior chat turns and the question are passed through
  `Pseudonymizer.Apply`, and the answer through `Pseudonymizer.Restore`, so the provider sees real names.
- **Guardrails** (`AssistantPrompts.System`): answer only from the data and say when it doesn't cover the question;
  safety (item 9) first; no diagnosis or medication advice; data is treated as information, not instructions;
  short answers. Answers are rendered as text (never HTML) with a "use your clinical judgment" note.
- Limits: question 800 characters, last 8 turns, 30-second timeout, friendly errors for bad key, rate limits and
  retired models. Audit log records who asked about which client and when, not the text.
- Provider choice: Groq is cheap and fast and does not train on API data; enable Zero Data Retention in Groq's Data
  Controls. Real PHI additionally needs a signed BAA with the AI provider, or a self-hosted open-weight model
  (any OpenAI-compatible server works by changing `Assistant:BaseUrl`).

## RTM rules engine (CY2026 CPT)

| Code | Rule implemented |
|---|---|
| 98975 | Set-up and education, once per episode, needs at least 2 data days |
| 98986 / 98978 | CBT device supply per rolling 30-day period from RTM start: 2-15 days / 16-30 days, never both |
| 98979 | Treatment management, 10-19 attested minutes in the calendar month |
| 98980 | First 20 minutes (replaces 98979) |
| 98981 | Each additional full 20 minutes (units) |

- Treatment-management codes require at least one **live** interaction (video, phone, in person) in the month.
  Secure messages are rejected as a live interaction.
- No rounding up ("halfway" rule does not apply). Only **attested** time counts.
- Nothing is billable without RTM consent on file.
- Device codes are reported for **completed** periods on the monthly report; the in-progress period is shown as status.
- All thresholds are constants in `RtmBillingRules`, so a rule change is a one-file edit.

## Provider time tracking (the reimbursable part)

1. On the client page, `clientDetailsController.js` counts seconds only while the tab is visible, focused, and
   the provider has interacted in the last 60 seconds. Pause/Resume is manual too.
2. Every 15 s it POSTs a heartbeat (`trackingKey`, `activeSeconds`) with the anti-forgery token; on tab hide it
   flushes with `navigator.sendBeacon`.
3. The server (`ReviewTimeRules.AcceptedIncrement`) caps each heartbeat at 20 s **and** at the wall-clock time since
   the last heartbeat, so time cannot be inflated or replayed.
4. Time lands as a *pending* `RtmTimeEntry`; the provider attests it (one confirmed click) before it counts.
   Manual entries require a note and an attestation checkbox. Every write is audit-logged by the real app.

## Extensibility (Remote Physiologic Monitoring), preview only

Per sponsor guidance, RPM is out of scope for this phase (focus is RTM); the Physiologic tab is a labeled preview.

`Observation` is a FHIR-like row (LOINC code, value, unit, time, source device, optional session link).
Heart rate today; HRV, SpO2 or sleep are new rows in `ObservationCatalog`, not new tables or screens.
The Physiologic tab renders any observation codes present. RPM billing (99453-99458) would be a sibling
calculator next to `RtmBillingCalculator`.

## Sign-in (demo stand-in)

Production Reflect XR signs in with Microsoft Entra External ID. The demo has a sign-in portal with test accounts and
an ASP.NET Core cookie session: HttpOnly, SameSite=Strict cookie; 30-minute sliding idle timeout (background polling
does not extend it); generic error messages; 5-attempt lockout for 5 minutes; constant-time password check; return URLs
limited to this site; sign-out by POST with anti-forgery. AJAX calls get 401/403 instead of HTML, and open pages send
the provider to sign-in when the session expires.

## Roles and minimum necessary

| Role | Policy | Sees |
|---|---|---|
| Provider | `ProviderOnly` | Their own clients only (ownership checked on every read, otherwise 404) |
| Facility Admin | `FacilityAdminOnly` | Per-provider billing counts for facilities they manage; no client names, identifiers or clinical values |
| End User | none | Access denied |

`ProviderRtmRollup` builds the admin numbers from counts only, so client data never reaches the admin ViewModel.
Per sponsor feedback (an admin could be anyone in IT), admins have no path to patient data.

## Access audit, notifications and outcomes

- **Audit trail.** `[AuditAccess(action)]` (an action filter) writes an `AccessAuditEntry` after each audited action:
  who, role, action, client id, a non-clinical detail (tab name, month) and success. Sign-in, failed sign-in and
  sign-out are logged by the account controller. Identical events within a minute collapse. Entries are append-only
  (`IAccessAuditRepository` has no update or delete). The Facility Admin sees record references (`Record 1AACEA`),
  never names; the treating provider sees the same history on the client's RTM tab.
- **Bell.** `ProviderNotificationRules` (pure, tested) turns flags and the RTM evaluation into notifications with
  stable keys, so "mark as read" sticks until the underlying fact changes. The count is polled every 60 s and on live
  data, without extending the session.
- **Outcomes.** `OutcomesCalculator` (pure, tested) compares the first and latest PHQ-9/GAD-7 in the window per
  client: improved/worsened by 5+, response (50%+ drop from 10 or more), remission (below 5). Shown as observational.

## Audience check-in (demo only)

`PhoneCheckInService` issues a 6-character code (no look-alike characters) tied to one provider and one client;
the public `/checkin` page knows only the code. Codes expire after 30 minutes or 60 check-ins; each phone (cookie)
may send 3 per 2 minutes; anti-forgery on the form. Submissions go through `IngestionService` like the API.
Not for production: real check-ins come from Reflect Mobile.

## HIPAA-minded choices

Provider can only load their own clients (ownership checked in every ViewModel, else 404); least-privilege
named policy; CSP blocks all external hosts; no PHI in URLs beyond opaque ids; CSV export neutralizes formula
injection; soft delete; synthetic data only. Production still needs a
BAA-covered stack (Azure SQL/App Service under Microsoft's BAA; BigQuery only for de-identified analytics).

## Merge steps

1. Copy `Areas/Provider`, `Models/ViewModels/ProviderDashboard`, `Models/Validation`, `Messages`,
   `wwwroot/css/provider-dashboard.css`, `wwwroot/js/{controllers,services,shared}/provider`, Chart.js lib,
   and the Domain `Entities/ProviderDashboard`, `Interfaces/Repositories`, `ProviderDashboard` folders.
2. Delete the stand-ins listed in the README; add EF configurations + migrations for the clinical entities.
3. Register the repositories in DI / `IUnitOfWork`; map `ProviderOnly` to the provider role; copy the menu JSON.
4. Move `tests/Reflect.Domain.Tests` into the real test project.
