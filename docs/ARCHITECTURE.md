# Architecture

## Goals that shaped the design

1. **Merge without a rewrite.** The prototype follows the Reflect XR extension spec: one Area, thin controllers, fat
   ViewModels, per-screen JS, repository interfaces, theme tokens, the CSP, and no CDN.
2. **RTM is a clinical workflow, not an analytics feature.** The dashboard records the provider's review time, documents
   interactive contact, and counts remote data days, which are the three things RTM billing depends on.
3. **Product analytics and clinical data stay separate.** Usage events (sessions, modules, mood ratings) and clinical
   records (assessments, goals, communications, monitoring time) are different entities behind different repositories.
4. **Data can come from anywhere.** Screens only see repository interfaces. Today they're in-memory; tomorrow they can be
   EF Core over Azure SQL, or a feed from the platform's own tracking (`MonitoringTimeSource.ExternalFeed`).
5. **Room for RPM.** Physiologic data is stored as generic `(metric code, value, time)` readings, so heart rate today and
   other measures later need no schema change.

## Layers

```
Reflect.Domain (class library, no framework dependencies)
  Common/                 BaseEntity, ICurrentUserService, EntityNotFoundException, US date helpers
  ProviderDashboard/
    Entities/             Provider, Patient, ActivitySession, Assessment, TreatmentGoal,
                          CareCommunication, MonitoringTimeEntry, PhysiologicReading
    Repositories/         one interface per entity (async, CancellationToken)
    Catalogs/             Reflect modules (XR + Mobile), physiologic metrics
    Rules/                RtmBillingCalculator, EngagementCalculator, AttentionRules,
                          AssessmentSeverity, GoalProgressCalculator, MonitoringTimePolicy, InsightBuilder
        ↑
Reflect.Web (ASP.NET Core MVC, .NET 10)
  Areas/Provider/         Controllers (thin) + Views (Razor)
  Models/ViewModels/Provider/<Screen>/   PrepareData / PrepareDataForSave
  Infrastructure/         policy name, repository facade, ProviderContext (user + clock), 404 filter
  Infrastructure/InMemory/   demo-only repositories + synthetic data seeder
  Messages/               all user-facing toast text
  Standins/               demo sign-in, ICurrentUserService, GetCspNonce, CSP header, menu loader
  wwwroot/js/controllers/provider/   one init(...) per screen + chart helper + timer
  wwwroot/js/services/provider/      monitoringService.js ($.ajax wrappers returning Promises)
  wwwroot/css/provider-dashboard.css scoped styles (rpd-*), built only on theme tokens
        ↑
Reflect.Domain.Tests (xUnit): 58 tests over the rules
```

The request flow for every screen is: controller → `vm.PrepareData(repos, context, ct)` → view. For saves:
`vm.PrepareDataForSave(...)` validates and returns a `<Screen>SaveData` object, and the controller persists it. ViewModels never save.

## Data model

| Entity | Kind | Key fields |
|---|---|---|
| `Patient` | clinical | provider, chart number, concern, status, enrolled on, **RTM consent date**, platforms, device |
| `ActivitySession` | product analytics | platform (XR/Mobile), **setting (remote/in clinic)**, module + pathway, start/end, mood before/after, creations, reflection |
| `Assessment` | clinical | PHQ-9 or GAD-7, score, completed at |
| `TreatmentGoal` | clinical | measure, direction, baseline/current/target, start/target date, status |
| `CareCommunication` | clinical / billing | channel (video, phone, secure message, in person), time, minutes, summary. `IsInteractive` = video or phone |
| `MonitoringTimeEntry` | billing | provider, patient, **source (auto/manual/external)**, start, last activity, **active seconds**, note |
| `PhysiologicReading` | RPM | metric code, value, time, session, context (start/end), device |

All timestamps are `DateTimeOffset` in UTC. Calendar logic (data days, billing months) uses US Eastern, matching the real app.

## Monitoring time (the billable part)

The time a provider spends reviewing a patient's data is what RTM treatment management bills for, so it has to be accurate:

- When a patient record is open, `monitoringTimerController.js` counts a second only if the tab is visible, the provider
  has interacted in the last 2 minutes, and tracking isn't paused. It creates an entry after 5 active seconds, so a quick
  glance isn't counted.
- Every 15 seconds it sends a heartbeat with the seconds counted since the last one. On page close, it sends a final
  heartbeat with `navigator.sendBeacon`; the antiforgery token travels in the form body.
- The server (`MonitoringTimePolicy.ApplyHeartbeat`) **never credits more than the wall-clock time since the last
  heartbeat (plus 5 seconds) and at most 60 seconds per call**, so a modified or stuck client can't inflate billable time.
- Provider-only checks: an entry can only be updated by the provider who owns it.
- Entries can't be edited or deleted in the UI. Manual entries need a note and are limited to the current month, so the
  log can support an audit. (The real app also audits every database write.)
- The "My monitoring time" screen and its CSV export let providers reconcile their own record against billing, as
  discussed in office hours.

## RTM rules (reference configuration)

All in `RtmBillingRules` / `RtmBillingCalculator`, tested in `RtmBillingCalculatorTests`:

| Code | Rule used |
|---|---|
| 98975 | Set-up and education: once, in the consent month, with 2+ data days |
| 98978 | Device supply, CBT monitoring: 16+ remote data days in the period |
| 98986 | Device supply, CBT monitoring: 2-15 remote data days |
| 98979 | Treatment management 10-19 min, with at least one interactive contact |
| 98980 | Treatment management first 20 min, with at least one interactive contact |
| 98981 | Each additional 20 min |

- A **data day** is a day with at least one **remote** session. In-clinic sessions don't count, which is why sessions
  record their setting.
- **Treatment minutes** are monitoring time plus interactive-contact minutes.
- The period is a US Eastern calendar month (`BillingPeriod`). Device-supply codes are formally "each 30 days". The
  calculator takes any start and end date, so switching to rolling 30-day periods only changes `BillingPeriod`.
- The readiness states are Ready, Partial, On track, Behind pace (16 days still possible but would need a session on more
  than 75% of the remaining days), At risk (16 days no longer possible), Not billable, and Not enrolled.
- Payer rules vary. The UI labels this as decision support and says to confirm with a billing team.

## Security and privacy

- Every screen requires the `ProviderOnly` policy. Every patient query is scoped to the signed-in provider
  (`GetForProviderAsync`); another provider's patient returns 404, not 403, so its existence isn't revealed.
- Every POST has `[ValidateAntiForgeryToken]`, including the AJAX and beacon calls.
- The real CSP header is sent on every response. Inline scripts carry a per-request nonce. There are no inline event handlers.
- CSV exports escape formula-like cells (`=`, `+`, `-`, `@`) to prevent spreadsheet injection.
- Synthetic data only; the seeder uses invented names and chart numbers.
- For production HIPAA readiness: Entra sign-in with MFA, Azure SQL encryption at rest and TLS in transit, the existing
  audit log, a BAA with every processor, minimum-necessary views, and session timeouts.

## Swapping in a database

The office-hours guidance allowed a real database. The spec asks for in-memory demos, so the prototype keeps in-memory
repositories and the interfaces are ready for either:

1. Add an EF Core `DbContext` with these entities (a `provider` schema, a global `IsDeleted` filter).
2. Implement the eight repository interfaces with EF Core (Azure SQL in production; PostgreSQL works the same for a
   standalone deployment).
3. Replace `AddProviderDashboardInMemoryData()` in `Program.cs`. No controller, ViewModel, or view changes.

Think of this store as a landing area: session events stay owned by the Reflect platform (Firebase Analytics / BigQuery
→ backend API), and the dashboard keeps only what it needs to display and document monitoring.

## Accessibility and theming

- Light and dark mode use only theme tokens. Chart colors are two tokens (`--rpd-series-xr`, `--rpd-series-mobile`)
  re-stepped from the brand indigo and coral so they pass lightness, chroma, color-blind separation, and 3:1 contrast checks
  in both modes.
- Charts redraw when the theme changes and each has a **Table** toggle. Every status pairs color with an icon and a label.
- Text and spacing use `rem`, so the accessibility panel's text-size setting reaches every screen. The color-blind mode
  attribute is honored.
- Tested at 390px, 820px, and 1440px with no horizontal scroll. Page buttons stack full-width on phones.
