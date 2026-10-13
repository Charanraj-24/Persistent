# Changelog

## 0.1.0 (2026-10-13)

First prototype of the Reflect Provider Dashboard extension.

### Added
- `Provider` Area with the Dashboard, Patients, Patient record (six tabs), Goals, Communications, My monitoring time, and RTM billing screens.
- Auto-tracked provider monitoring time on the patient record, with idle detection, pause/resume, and a server-side cap on each heartbeat.
- Manual monitoring-time entries (with a required note) and CSV exports for monitoring time and RTM billing.
- RTM billing rules (CPT 98975, 98978, 98979, 98980, 98981, 98986) as a tested, framework-free class.
- Rule-based attention flags (inactivity, engagement drop, PHQ-9/GAD-7 change, mood drop, billing gaps) and plain-language talking points.
- Physiologic (RPM) preview tab, with heart rate and HRV stored in a generic metric model so new measures need no schema change.
- Domain entities and repository interfaces, in-memory implementations, and a deterministic synthetic data seeder.
- Stand-ins for the layout, theme tokens, shared JS services, demo sign-in, `ICurrentUserService`, and `GetCspNonce`, plus the real CSP header.
- 58 xUnit tests for the domain rules and a RemixIcon class checker (`tools/check-remixicons.sh`).

### Library added beyond the spec
- Chart.js 4.4.7 (self-hosted).
