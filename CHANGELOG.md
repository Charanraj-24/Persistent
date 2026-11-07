# Changelog

## 0.9.0 - 2026-11-07
- Client page: "Latest from Reflect" card with the newest sessions, questionnaires and readings. Items that arrived
  live in the last 30 minutes are marked New with their source (Live demo, Audience phone, Reflect app, Simulator)
  and flash; the card and the RTM status strip refresh within about 5 seconds of new data.
- Records now carry their ingestion source. Live demo events are labeled "live demo" instead of "simulator".
- Live data demo: an "Open client" link for the chosen client, and the choice is remembered.
- Client notes: Notes tab (add, filter by topic, pin, edit, delete, author only), an "Add note" button on the client
  page and session prep, note buttons with counts on each session, questionnaire and artwork, and a billing note on
  the RTM tab. Notes are visible to providers who can open the client, never to the Facility Admin, never sent to the
  AI, soft-deleted, and adding, editing and deleting is audited (without the text). Session prep shows the latest 3.

## 0.8.0 - 2026-11-06
- Official module catalog from PTI's Reflect Product Catalog (9/9/2026): 14 Reflect Mobile modules in 4 groups and
  17 Reflect XR modules in 5 programs, including the Training Tutorial. Synthetic data, live demo, phone check-in
  (grouped list), API validation, charts, AI context and talking points all use it.
- Module usage tab on the dashboard: who uses each module most and what each client uses most, with period, app,
  group, measure and search filters, sort orders, and a client heat map. Updates in place on live data.
- Training Tutorial: status on the Engagement tab, a note-level flag and talking point for XR users who haven't done it.
- Late-night use (midnight to 5 AM, 2+ sessions in 14 days): warning flag, bell notification, talking point, and a
  separate "Late night" bar on the time-of-day chart.
- Engagement tab: time by module or by group, stat strip with tutorial and late-night status.
- PTI's Reflect XR logo on the sign-in page, sidebar and phone pages.

## 0.7.0 - 2026-11-06
- AI assistant opens and closes with Ctrl + Shift (pressed together and released, no other key) on the dashboard,
  clients list, client page and session prep. It opens as a centered panel with a wave across the screen; Esc and the
  close button also close it. The "Ask AI" buttons are replaced by a shortcut hint.
- Conversations are kept on the server per provider: one thread for the caseload, one per client (shared between the
  client page and session prep). Survives closing, reloading and navigating. "Clear chat" deletes a thread.
- Only complete question-and-answer pairs from the saved thread are sent back to the AI as context, re-de-identified.

## 0.6.0 - 2026-11-04
- Reimbursement estimate (CMS CY2026 national average, non-facility) on the dashboard, client page, monthly report and
  Facility Admin view; device codes 98978/98986 flagged as locally priced.
- Session prep page and AI-drafted RTM note (template first, AI polish when configured, numbers kept).
- Outcomes tab: PHQ-9/GAD-7 improved 5+, response (50%+ drop from 10+), remission (below 5), before vs. after and
  engagement vs. change charts. `OutcomesCalculator` in the domain, tested.
- Artwork tab: timeline of pieces with module, mood change and emotion words (generated SVG placeholders in the demo).
- Access audit log: sign-ins and failures, record opens, tabs, exports, AI use, attestations; append-only; Facility
  Admin view with record references only; per-client access history on the RTM tab.
- Notification bell: urgent flags, clinical warnings, RTM threshold reminders, pending attestations; mark as read.
  `ProviderNotificationRules` in the domain, tested.
- QR audience check-in: join code and QR on the Live page, public mobile check-in page, expiry, caps, rate limits.
- Hosting: Dockerfile, forwarded-headers option for proxies/tunnels, `Demo:PublicBaseUrl`, generic error page.

## 0.5.0 - 2026-11-04
- Sign-in portal (stand-in for Entra): split-screen page, demo accounts, cookie session with 30-minute idle timeout,
  lockout, safe return URLs, sign-out; AJAX gets 401/403; expired sessions return to sign-in.
- Visual refresh within the PTI palette: hairline cards and soft elevation, underline tabs, lighter tables, pill
  badges, refined sidebar with an original brand mark (placeholder for PTI's logo).
- Watercolor hero band on the dashboard and client page; segmented stat strips replace boxed KPI cards.

## 0.4.0 - 2026-11-03
- Facility Admin role (demo user Pat Morgan) with its own `FacilityAdminOnly` policy and sidebar.
- RTM billing by provider screen: counts per provider, documentation gaps, charts, CSV. No client data.
- `ProviderRtmRollup` (domain, tested) and `IFacilityAccessService` contract (stand-in for the demo).
- Sponsor feedback: Physiologic tab labeled as an RPM preview; simulator favors RTM data.

## 0.3.0 - 2026-11-03
- Ingestion API (`/api/ingest/sessions|assessments|observations`, API-key auth, validation) and `IngestEvent` audit trail.
- Live updates: open pages poll a provider-scoped feed; toasts (urgent for PHQ-9 item 9), KPI tiles, client status
  strip and the open tab refresh automatically; "Live" indicator.
- Live data demo page: scenario buttons, auto-simulator, incoming-data log, curl example.
- Clinical assistant chat on the dashboard (caseload) and client page (one client), via any OpenAI-compatible API
  (default Groq, `openai/gpt-oss-20b`), with de-identified context, pseudonymized names, guardrails and offline fallback.
- Tests for the pseudonymizer and for identifiers never appearing in AI context.

## 0.2.0 - 2026-11-02
- Dashboard and client page split into lazy-loaded tabs (one request per tab on first open; last tab remembered).
- One ViewModel per tab, each loading only its own data; new endpoints `Home/Tab` and `Clients/Tab/{id}`.
- New views: 12-week engagement trend, time-of-day and weekday patterns, XR vs. Mobile split, stress before/after,
  cumulative attested minutes vs. thresholds, RTM readiness by data-day tier and status, report summary charts.
- In-card view toggles (charts / table and similar) via `_ViewToggle`.
- Monthly report split into Summary and Clients tabs.
- One ViewModel class per file.

## 0.1.0 - 2026-10-30
- New `Provider` Area: Dashboard, Clients, Client detail (5 tabs), Add time / live interaction, Monthly RTM report, RTM documentation.
- Domain: provider dashboard entities, repository interfaces, RTM billing engine (CY2026 CPT 98975/98978/98979/98980/98981/98986), engagement flags, assessment scoring, talking points.
- Active review-time tracker with server-side anti-inflation rules and attestation.
- In-memory repositories with deterministic synthetic data (24 clients, 2 providers).
- Stand-ins per tech spec: layout shell, theme tokens, MessageService and helpers, fake user, CSP nonce.
- xUnit tests (63 cases) for billing, periods, time rules and clinical flags.
- Added library: Chart.js 4.4.7.
