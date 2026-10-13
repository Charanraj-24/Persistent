# Project Summary: Reflect Provider Dashboard

**Challenge X · Persistent Technology, Inc. · Team Charisma**

## The problem

Reflect XR (VR art-making) and Reflect Mobile give patients therapeutic creative tools between sessions, but providers
can't see how patients use them, and they can't get paid for reviewing that data. Without reimbursement, providers have
little reason to monitor between visits, and fewer patients get access. CPT's **Remote Therapeutic Monitoring (RTM)**
codes pay for this work. They require proof of three things each period: enough days of remote data, enough provider
treatment-management time, and an interactive contact with the patient.

## What we built

A web-based provider dashboard, built as a drop-in extension of the Reflect XR web app (.NET 10 MVC, Razor, jQuery,
Bootstrap), with synthetic data.

- **Caseload dashboard.** Who needs attention today and why; weekly XR vs. Mobile engagement; module usage; RTM progress
  for every enrolled patient.
- **Patient record.** Sessions (platform, module, duration, remote or in clinic, mood before and after), PHQ-9 and GAD-7
  trends with severity bands, treatment goals with progress against the timeline, plain-language talking points for the
  next session, and a communication log.
- **Monitoring time that holds up to an audit.** A visible timer records only active, visible review time, pauses when
  the provider is idle, can be paused by hand, and is capped by the server. Manual entries need a note. Providers get their
  own log and CSV export to reconcile against billing.
- **RTM billing support.** For each patient and month: remote data days on a calendar, a minutes meter with the 10, 20,
  40, and 60-minute thresholds, whether an interactive contact has happened, eligible CPT codes (98975, 98978/98986,
  98979, 98980, 98981), and what's still missing. Exportable as CSV.
- **RPM-ready.** A physiologic tab shows heart rate and HRV at the start and end of VR sessions. Readings are generic, so
  new measures are a catalog entry, not a new table.

## Key decisions

| Decision | Why |
|---|---|
| Follow the tech spec (.NET MVC + jQuery) rather than React | The sponsor allowed either, but matching the spec means the PTI team can merge with a file copy instead of a port |
| Repository interfaces with in-memory data | The spec asks for it. The interfaces make swapping in Azure SQL or PostgreSQL, or a live feed, a single registration change |
| Record session setting (remote / in clinic) | Only remote sessions count toward RTM, per the sponsor |
| Document communications, don't host them | The sponsor said conversations happen elsewhere (Zoom, phone); the dashboard only needs to record them |
| Track the provider's time per patient, against a calendar month with 10/20/40/60-minute thresholds | That's what is billed, and the sponsor asked for those thresholds |
| Rule-based flags and talking points, not an LLM | Every sentence traces back to data on the page; no PHI leaves the app; no API cost. An LLM can be added behind the same interface later |
| Server-side cap on tracked time | Billable time must not depend on trusting the browser |

## Quality

- 58 xUnit tests cover the billing rules, periods, severity bands, goal progress, engagement, alerts, and timer limits.
- End-to-end browser checks: the timer records and pauses; all forms save and validate; CSV exports work; the CSP header
  is sent; a POST without an antiforgery token is rejected; a provider can't open another provider's patient (404);
  a non-provider is denied.
- Light and dark mode, phone/tablet/desktop with no horizontal scroll, text scaling in `rem`, status never shown by
  color alone, and a table view for every chart. Chart colors are validated for color-blind separation.

## Limitations and next steps

1. Connect real data. Map Firebase Analytics / BigQuery session events to `ActivitySession`, and assessments to
   `Assessment`, through the backend API. Add EF Core repositories.
2. Validate the RTM rules with PTI's billing and compliance team. Add payer-specific settings and rolling 30-day periods
   if needed.
3. Clinical review of flag thresholds with Dr. Bhargava. Add a PHQ-9 item 9 safety flag and an escalation workflow.
4. Patient-facing consent capture, and a provider note that combines talking points and time into a billing note.
5. RPM phase: live device pairing and RPM codes, reusing the generic reading model.
6. Production hardening for HIPAA: Entra MFA, audit review screens, session timeouts, a BAA for every processor.
