# Reflect Provider Dashboard (Challenge X extension)

A HIPAA-minded provider dashboard for **Reflect XR** and **Reflect Mobile**, built as a standalone .NET 10 MVC
extension that follows the *Reflect XR Web App: Tech Spec for Extension Teams* (v1.12.4) so it can be merged
into the real web app as a file copy. **All data is synthetic.**

It turns client engagement in Reflect into **Remote Therapeutic Monitoring (RTM)**: it tracks the provider's review
time automatically, tells them which CPT codes their documentation supports, and produces month-end documentation.

## Run it

```bash
dotnet build
dotnet run --project src/Reflect.Web          # http://localhost:5080
dotnet test                                   # xUnit tests for the RTM and clinical rules
```

No database, secrets or external services are needed. Offline build without NuGet:
`dotnet build src/Reflect.Web -p:RazorRuntimeCompilation=false`.

### Turn on the AI assistant (optional)

The assistant works with any OpenAI-compatible API; the default is **Groq** with `openai/gpt-oss-20b`
(Groq retired its Llama models for free/developer accounts on 8/16/2026 and recommends gpt-oss as the replacement).
Create a key at console.groq.com, then keep it out of the code with user-secrets:

```bash
dotnet user-secrets set "Assistant:ApiKey" "gsk_your_key_here" --project src/Reflect.Web
# optional: bigger model
dotnet user-secrets set "Assistant:Model" "openai/gpt-oss-120b" --project src/Reflect.Web
```

Without a key the chat still opens and shows the data summary it would have sent, labeled "AI model not connected".

Open http://localhost:5080 and **sign in** with a demo account (password `reflect-demo`, set in `Demo:Password`).
Click an account on the sign-in page to fill its email. You can also switch accounts from the profile menu (top right): **Dr. Maya Ellison** (provider, 18 clients),
**Jordan Reyes** (provider, 6 clients), **Pat Morgan** (Facility Admin: billing totals only, no client data),
**Sam Taylor** (end user, gets Access denied).

## Demo script (about 3 minutes)

1. Open **Dashboard** in one window and **Live data demo** (sidebar) in another.
2. On Live data demo pick a client and click **VR session, mood improves**. Within ~5 seconds the dashboard shows a
   toast and its numbers and open tab refresh.
3. Click **PHQ-9, item 9 endorsed** for the same client. Every open page shows an urgent toast and the client moves
   to the top of *Needs attention*.
4. Open that client. Press **Ctrl + Shift** (together, then release) and ask "Are there any safety concerns?" The answer comes only from that
   client's data; names and identifiers never leave the server.
5. Switch to **Pat Morgan (Facility Admin)** from the profile menu: billing totals per provider with no client names,
   and every clinical page returns Access denied.
6. Start the **auto-simulator** to keep data flowing while you present the RTM & billing tab and the monthly report.
7. Optional: show the API with the curl example on the Live data demo page (`X-Reflect-Ingest-Key`).
8. **Audience check-in:** on Live data demo pick a client and click **Show QR code**. People in the room scan it and
   send a 20-second check-in; each one lands in the feed and on that client's page (see "Audience check-in" below).
9. Show **Outcomes** (dashboard tab), the client's **Artwork** tab, **Prep for session** with the AI-drafted note,
   the **bell** (urgent flags, RTM reminders) and, as Pat Morgan, the **Access audit log**.

## Audience check-in (QR code)

The Live data demo page shows a QR code and a 6-character join code for one demo client you pick. Phones open
`/checkin`, choose an activity, set mood before/after and stress, pick up to 3 emotion words and send. The page
never shows the client's name or id. Codes expire after 30 minutes or 60 check-ins, each phone is limited to 3
check-ins per 2 minutes, and **Stop** ends the code at once. Data goes through the same `IngestionService` as the
API, with source "audience phone".

Phones can't reach `localhost`, so the app must be reachable from the phones. Pick one:

| Option | How | Notes |
|---|---|---|
| **Azure App Service** (most reliable on stage) | Deploy the Docker image (below) or `dotnet publish`; set `Hosting__BehindProxy=true` | Laptop and venue Wi-Fi independent; data resets when the app restarts |
| **Cloudflare quick tunnel** | `cloudflared tunnel --url http://localhost:5080`, then run the app with `Hosting__BehindProxy=true` and `Demo__PublicBaseUrl=https://<name>.trycloudflare.com`, and open the dashboard through that URL | No account needed, no warning page for phones |
| **Same Wi-Fi** | `dotnet run --project src/Reflect.Web --urls http://0.0.0.0:5080`, open `http://<laptop-ip>:5080` | Works only if the venue network allows device-to-device traffic |

If the page is opened on `localhost`, the QR card shows a warning instead of silently failing. Take a screenshot of
the QR card for the slide deck as a backup, and keep a printed join code handy.

## Host it

```bash
docker build -t reflect-dashboard .
docker run -p 8080:8080 -e Demo__Password=choose-a-password reflect-dashboard   # http://localhost:8080
```

The image runs in Production with strict CSP (`Csp__StrictScripts=true`) and trusts proxy headers
(`Hosting__BehindProxy=true`) so cookies are Secure and the QR code uses the public https URL. Pass the AI key as an
environment variable (`Assistant__ApiKey`) or an App Service setting, never in the image. For Azure App Service
(Linux, container or code): set the same settings, enable HTTPS only, and use one instance (state is in memory).

## Screens (Area `Provider`; policies `ProviderOnly` for clinical screens, `FacilityAdminOnly` for billing totals)

| Screen | URL | What it does |
|---|---|---|
| Dashboard | `/Provider/Home/Dashboard` | KPI tiles, then lazy tabs: Needs attention (safety first, plus progress), RTM to-dos, Caseload trends (activity by day / by experience, RTM readiness by data-day tier / status), Recent activity |
| Clients | `/Provider/Clients/Index` | Caseload table: flags, data days toward 16, minutes toward 20, live contact, status, next step |
| Client detail | `/Provider/Clients/Details/{id}` | Header, RTM status strip and review-time tracker, then lazy tabs (each loaded on first open from `/Provider/Clients/Tab/{id}?name=`): Overview (talking points, flags, by day / by app), Engagement (Training Tutorial status, late-night use; charts: 12-week trend, time by module or group, time of day with late night split out, weekday / session log), Therapeutic response (mood or stress before vs. after, PHQ-9/GAD-7 / history), Physiologic (chart / measures), RTM & billing (meters, completed periods, data-day calendar, time log / month progress chart) |
| Add time / live interaction | `/Provider/TimeEntries/Create?clientId={id}&activity=3` | Manual treatment time or live interaction, attested on save |
| Monthly RTM report | `/Provider/Reports/Monthly?month=yyyy-MM` | Tabs: Summary (minutes by client vs. 20-min line, codes / status) and Clients table; CSV export |
| RTM documentation | `/Provider/Reports/ClientSummary/{id}?month=yyyy-MM` | Printable per-client monthly documentation with signature line |
| RTM billing by provider *(Facility Admin)* | `/Provider/FacilityBilling/Index?month=yyyy-MM` | Per-provider counts: clients on RTM, codes ready, attested minutes, live-contact coverage, documentation gaps; charts; CSV. No client names or clinical data |
| Live data demo | `/Provider/Live/Index` | QR audience check-in, send app events for a client, run the auto-simulator, watch incoming data, API example |
| Module usage *(dashboard tab)* | `/Provider/Home/Tab?name=usage` | Who uses which Reflect module most. Two views: modules (pick one, see its clients ranked) and clients (pick one, see their modules ranked); filters for period (7/30/90 days), app, group, minutes or sessions, and search; sort orders on both lists; a client-by-group (or by-module) heat map with sortable columns. Updates in place when live data arrives, keeping filters |
| Outcomes *(dashboard tab)* | `/Provider/Home/Tab?name=outcomes` | Measurement-based care: PHQ-9/GAD-7 improved, response, remission, before vs. after, engagement vs. change |
| Artwork *(client tab)* | `/Provider/Clients/Tab/{id}?name=art` | Timeline of shared pieces with module, mood change and emotion words (demo images are generated placeholders) |
| Session prep | `/Provider/Clients/Prep/{id}` | One-page brief before a session, AI-drafted RTM note |
| Notifications | header bell | Urgent flags, PHQ/GAD worsening, inactivity, RTM thresholds near period/month end, time waiting for attestation |
| Access audit log *(Facility Admin)* | `/Provider/AccessLog/Index` | Who opened which record and when, sign-ins and failures, exports; record references only, no client names |
| Phone check-in *(public, demo)* | `/checkin?code=` | Anonymous check-in page for the audience; protected by join code, caps and rate limits |
| Clinical assistant | **Ctrl + Shift** on Dashboard, Clients, Client detail and Session prep | Centered chat about one client or the whole caseload, grounded only in their data. Conversations are saved per provider (one thread for the caseload, one per client); Clear chat deletes one |

Ingestion API (machine-to-machine, header `X-Reflect-Ingest-Key`, key in `Ingest:ApiKey`):
`POST /api/ingest/sessions`, `POST /api/ingest/assessments`, `POST /api/ingest/observations`.

AJAX/POST endpoints: `Live/Feed?after=`, `Live/RunScenario`, `Live/StartCheckIn`, `Live/StopCheckIn`, `Live/CheckInStatus`,
`Notifications/Count`, `Notifications/List`, `Notifications/MarkAllRead`, `Assistant/DraftNote`, `Assistant/Ask`, `Home/Kpis`, `Clients/Status/{id}`, `Home/Tab?name=`, `Clients/Tab/{id}?name=`, `TimeEntries/Heartbeat`, `TimeEntries/AttestPending`, `TimeEntries/DiscardPending`, `Reports/ExportCsv`.
Sidebar items for merge: `src/Reflect.Web/menus/provider-menu.json`.

## Entities and interfaces added (Domain project)

Entities (all inherit `BaseEntity`): `ProviderProfile`, `ProviderClient`, `AppSession` *(product analytics)*,
`MoodCheckIn`, `AssessmentResult`, `RtmTimeEntry`, `Observation` *(generic code/value/unit, for RPM)*, `IngestEvent`.

Repositories: `IProviderProfileRepository`, `IProviderClientRepository`, `IAppSessionRepository`,
`IMoodCheckInRepository`, `IAssessmentResultRepository`, `IObservationRepository`, `IRtmTimeEntryRepository`, `IIngestEventRepository`.

Pure logic (xUnit-tested): `RtmBillingCalculator`, `RtmPeriods`, `RtmClientEvaluator`, `ReviewTimeRules`,
`EngagementAnalyzer`, `AssessmentScoring`, `TalkingPointsBuilder`, `ClinicalContextBuilder`, `Pseudonymizer`.

## Libraries beyond Part 2

- **Chart.js 4.4.7** (`wwwroot/assets/libs/chartjs/`, MIT): charts. Loaded only by Provider pages via `@section Scripts`.

Everything else is from the spec list at the spec's versions, hosted locally (jQuery 3.7.1, Bootstrap 5.3.3,
DataTables 1.13.8 + Responsive 2.5.0, Toastr 2.1.4, SweetAlert2 11.26, RemixIcon 4.2, jquery.validate 1.21.0 +
unobtrusive 4.0.0, IBM Plex from IBM's official npm release).

## What to delete on merge (stand-ins)

- `src/Reflect.Domain/Standins/` (BaseEntity, ICurrentUserService, EntityNotFoundException, date helpers)
- `src/Reflect.Web/Standins/` (sign-in portal and cookie session, CurrentUserService, facility-access stand-in, CSP nonce, menu reader, exception filter, stand-in views)
- `src/Reflect.Web/Views/Shared/_Layout.cshtml`, `wwwroot/standins/` (theme lookalike, sign-in styles)
- `src/Reflect.Web/Data/InMemory/` (replace with EF Core repositories + a BigQuery-backed `IAppSessionRepository`)
- `src/Reflect.Web/Services/Simulation/` and the Live data demo screen (demo only); keep `Api/IngestController` + `IngestionService` (or move them to Reflect.Api)

Then point the `ProviderOnly` policy at the real provider role and `FacilityAdminOnly` at `Facility_Admin`
(the spec asks for one policy; the second exists on purpose so billing staff never get clinical access, per sponsor feedback).
Copy `menus/facility-admin-menu.json` into the Facility Admin sidebar. See `docs/ARCHITECTURE.md`.

## Spec compliance checklist

English/US formats (M/d/yyyy, 12-hour, US Eastern), light and dark mode via tokens, rem sizing,
status never by color alone, phone/tablet/desktop without horizontal scroll, existing data read-only,
nav-tabs for multi-section screens, one-page create form, shared JS service names, RemixIcon classes verified
against v4.2, no inline handlers, every inline script has a CSP nonce, tested under the stricter nonce-only policy
(`Csp:StrictScripts=true`), anti-forgery on every POST, soft delete, `DateTimeOffset` in UTC, no `string?`,
user-facing text in `ProviderDashboardMessages`.

## Reflect module catalog

Module names follow PTI's "Reflect Product Catalog: Official Modules & Pricing" (effective 9/9/2026), in
`src/Reflect.Domain/ProviderDashboard/Catalogs.cs`. Use these keys as `moduleKey` in `POST /api/ingest/sessions`.

| App | Group | Modules (key) |
|---|---|---|
| Reflect Mobile | Creative Calm | Floating Bubbles (`m-floating-bubbles`), Finger Painting (`m-finger-painting`), Pour Paint (`m-pour-paint`), Breathing Mandala (`m-breathing-mandala`), Photo Coloring (`m-photo-coloring`), AI Created Mandala (`m-ai-mandala`) |
| Reflect Mobile | Create | Create Your Own Prompt (`m-create-own-prompt`), Options Prompt Pathway (`m-options-prompt`), Select from Images Pathway (`m-select-images`) |
| Reflect Mobile | Learn | Creativity (`m-learn-creativity`), Emotions (`m-learn-emotions`), Self (`m-learn-self`), Journey (`m-learn-journey`) |
| Reflect Mobile | Reflect | Reflection Journals (`m-reflection-journals`) |
| Reflect XR | Creative Calm | Containment (`xr-containment`), Rhythmic Relaxation (`xr-rhythmic-relaxation`), Color Psychology (`xr-color-psychology`), Flow (`xr-flow`) |
| Reflect XR | Explore Emotions | Emotional Expression (`xr-emotional-expression`), Self Compassion (`xr-self-compassion`), Catharsis (`xr-catharsis`), Portal to the Subconscious (`xr-portal-subconscious`) |
| Reflect XR | Explore Self | Present Self Outside (`xr-present-self-outside`), Present Self Inside (`xr-present-self-inside`), Personal Strengths (`xr-personal-strengths`), Future Self (`xr-future-self`) |
| Reflect XR | Explore Journey | Appreciation of Life (`xr-appreciation-of-life`), Hope Tree (`xr-hope-tree`), New Pathways (`xr-new-pathways`), Finding Balance (`xr-finding-balance`) |
| Reflect XR | Getting Started | Training Tutorial (`xr-tutorial`) |

The logo mark and lockup in `wwwroot/images/brand/` are cut from the logo in that catalog (300 px source).
Replace them with the vector files from PTI's Visual Identity Guidelines when available.
