# Reflect Provider Dashboard: Project Summary

**Team Charisma, Challenge X (Persistent Technology, Inc.)**

## Problem

Reflect XR and Reflect Mobile help clients process emotions through art, but providers can't see what happens
between sessions, and they aren't paid for the time it takes to look. CPT **Remote Therapeutic Monitoring (RTM)**
codes pay providers for monitoring therapy data and for the time spent managing it. That only works if the dashboard
captures the right data and the provider's time in a way that holds up to an audit.

## What we built

A provider dashboard that treats RTM as a **clinical workflow**, not an analytics page:

1. **Know who needs you.** Clients are ranked by clinical flags: safety first (PHQ-9 item 9), then a 5+ point
   worsening on PHQ-9 or GAD-7, inactivity and dropping engagement, plus progress flags.
2. **Walk into the session prepared.** Per-client talking points (for example "44 minutes in Explore Journey this
   week; most chosen emotion word 'grateful'") and an **AI assistant** that answers questions about one client or
   the whole caseload, using only their data.
3. **See therapeutic response, not just usage.** Per-client engagement by app, module, week, time of day and weekday;
   mood and stress before vs. after every session; PHQ-9/GAD-7 trends.
4. **Get paid for the review.** The dashboard tracks the provider's *active* review time per client (idle and
   hidden-tab time excluded, server-side anti-inflation checks). The provider attests it, and a rules engine shows
   exactly which codes are supported and what's missing: "3 min to 98980", "log a live interaction".
5. **Close the month.** Monthly report with charts and CSV, printable per-client documentation, and a **Facility
   Admin** view of billing totals per provider, with no access to client data.
6. **Show it working.** An ingestion API (what the Reflect apps would call), a live demo page and an
   auto-simulator; open pages update within seconds and urgent events alert every open page. A QR code lets the
   audience send real check-ins from their phones during the demo.
7. **Prove it helps.** An Outcomes view (PHQ-9/GAD-7 response and remission, engagement vs. change), an Artwork
   timeline, a one-page session prep with an AI-drafted RTM note, an estimated reimbursement figure, a notification
   bell for urgent flags and billing thresholds, and an access audit log.

## Billing rules implemented (CY2026 CPT)

98975 set-up; **98986/98978** CBT device supply for 2-15 / 16-30 data days per 30-day period (billed when the period
ends); **98979** 10 minutes, **98980** first 20 minutes and **98981** each additional 20 minutes of treatment
management per calendar month. Management codes require a live interaction and count only attested time, never
rounding up; nothing is billable without consent. 86 automated tests cover thresholds, period rollover, time-tracking
guards, clinical flags, facility rollups and AI privacy.

## Privacy and security

- **Least privilege:** providers see only their own clients; Facility Admins see counts and an access audit log
  with record references only; end users are denied. Every record view, export and AI request is audited.
- **AI de-identification:** the AI receives tokens instead of names, age bands instead of birthdates and relative
  days instead of dates, with no MRN, payer or NPI. Names are restored only on the server.
- **Server-side only:** strict Content Security Policy (no external hosts), anti-forgery on every form, API keys in
  user-secrets, API-key auth with constant-time checks for ingestion, soft delete, synthetic data only.

## Architecture

A .NET 10 MVC Area built to PTI's extension spec (thin controllers, fat ViewModels, repository interfaces, theme
tokens, CSP nonces, shared JS service names), so merging is mostly a file copy. Pages load tab by tab. Product
analytics (Firebase to BigQuery) stay separate from clinical data (Azure SQL). Physiologic data uses a generic
code/value observation model, so RPM can be added later without a rebuild. The AI uses any OpenAI-compatible API
(Groq `openai/gpt-oss-20b` by default).

## Next steps / questions for PTI

Confirm the real Reflect Mobile module catalog, the Firebase event names and which in-app check-ins (mood,
PHQ-9/GAD-7) are feasible. Map `ProviderOnly` and `FacilityAdminOnly` to Entra roles. Validate RTM eligibility with
PTI's billing advisor (98978/98986 are contractor-priced and require a qualifying device). Sign a BAA with the AI
provider, or self-host the model, before any real patient data. Pilot with 2-3 clinicians to tune the flags.
