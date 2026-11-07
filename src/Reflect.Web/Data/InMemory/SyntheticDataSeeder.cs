// DEMO ONLY: generates realistic, fully synthetic data relative to "today", so the demo always looks current.
// No real patient or personal data. Names, MRNs and NPIs are invented.
using Reflect.Domain.Helpers;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Standins.Security;

namespace Reflect.Web.Data.InMemory;

public static class SyntheticDataSeeder
{
    private sealed record Persona(
        string First, string Last, int Age, string DxCode, string DxLabel, string Payer, string Goals,
        double ActiveDayProbability, double XrShare, double EngagementTrend, int InactiveLastDays,
        int PhqStart, int PhqEnd, int GadStart, int GadEnd, bool SafetyItem, double MoodLift,
        int RtmStartDaysAgo, bool Consent, int MonthMinutes, bool LiveCall, int PendingMinutes,
        bool Wearable, ClientRtmStatus Status, int? ForcedPeriodDataDays = null);

    private static readonly Persona[] EllisonClients =
    {
        new("Avery", "Collins", 34, "F33.1", "Major depressive disorder, recurrent, moderate", "Medicare Part B", "Reduce depressive symptoms; rebuild daily routine.",
            0.75, 0.70, 0, 0, 16, 9, 11, 7, false, 1.6, 140, true, 44, true, 0, false, ClientRtmStatus.Active),
        new("Priya", "Natarajan", 29, "F41.1", "Generalized anxiety disorder", "Commercial PPO", "Use grounding skills when worry spikes.",
            0.65, 0.20, 0, 0, 12, 10, 15, 11, false, 1.2, 95, true, 23, true, 0, false, ClientRtmStatus.Active),
        new("Marcus", "Bell", 47, "F32.1", "Major depressive disorder, single episode, moderate", "Medicare Part B", "Re-engage with valued activities.",
            0.60, 0.55, -1, 0, 10, 15, 9, 12, false, 0.6, 180, true, 17, true, 0, false, ClientRtmStatus.Active),
        new("Elena", "Ruiz", 26, "F43.10", "Post-traumatic stress disorder, unspecified", "Medicaid MCO", "Process trauma memories safely; improve sleep.",
            0.50, 0.60, 0, 0, 14, 19, 14, 16, true, 0.4, 120, true, 12, true, 0, false, ClientRtmStatus.Active),
        new("Theo", "Nakamura", 52, "F43.23", "Adjustment disorder with mixed anxiety and depressed mood", "Commercial PPO", "Adjust to retirement; reduce isolation.",
            0.50, 0.50, 0, 9, 9, 8, 8, 8, false, 1.0, 75, true, 8, false, 0, false, ClientRtmStatus.Active),
        new("Grace", "Okafor", 38, "F41.1", "Generalized anxiety disorder", "Medicare Part B", "Lower physical tension; practice self-compassion.",
            0.70, 0.65, 0, 0, 8, 5, 13, 4, false, 1.4, 160, true, 26, false, 0, true, ClientRtmStatus.Active),
        new("Daniel", "Kim", 31, "F32.0", "Major depressive disorder, single episode, mild", "Commercial PPO", "Establish a baseline and build engagement.",
            0.80, 0.40, 0, 0, 11, 11, 9, 9, false, 1.1, 5, false, 6, false, 0, false, ClientRtmStatus.Active),
        new("Sofia", "Marchetti", 43, "F41.0", "Panic disorder", "Medicare Part B", "Reduce panic frequency; tolerate body sensations.",
            0.50, 0.50, 0, 0, 9, 7, 16, 12, false, 1.3, 58, true, 14, true, 0, false, ClientRtmStatus.Active, 14),
        new("Henry", "Walsh", 61, "F33.0", "Major depressive disorder, recurrent, mild", "Medicare Part B", "Maintain gains; notice early warning signs.",
            0.70, 0.90, 0, 0, 8, 5, 6, 4, false, 2.0, 210, true, 31, true, 0, true, ClientRtmStatus.Active),
        new("Imani", "Brooks", 24, "F32.1", "Major depressive disorder, single episode, moderate", "Medicaid MCO", "Increase pleasant activities; challenge self-criticism.",
            0.70, 0.45, 1, 0, 18, 7, 15, 7, false, 1.8, 130, true, 21, true, 0, false, ClientRtmStatus.Active),
        new("Lucas", "Ferreira", 36, "F41.1", "Generalized anxiety disorder", "Commercial PPO", "Build a consistent practice between sessions.",
            0.22, 0.30, 0, 0, 7, 7, 12, 11, false, 0.8, 100, true, 0, false, 0, false, ClientRtmStatus.Active),
        new("Nora", "Lindqvist", 57, "F43.23", "Adjustment disorder with mixed anxiety and depressed mood", "Medicare Part B", "Cope with caregiver stress.",
            0.65, 0.50, 0, 0, 12, 9, 12, 9, false, 1.5, 150, true, 15, true, 7, true, ClientRtmStatus.Active),
        new("Omar", "Haddad", 45, "F33.1", "Major depressive disorder, recurrent, moderate", "Commercial PPO", "Reduce rumination; improve sleep hygiene.",
            0.80, 0.60, 0, 0, 15, 7, 10, 6, false, 1.4, 200, true, 62, true, 0, false, ClientRtmStatus.Active),
        new("Chloe", "Bennett", 33, "F41.1", "Generalized anxiety disorder", "Commercial PPO", "Paused during travel; resume next month.",
            0.15, 0.10, -1, 12, 9, 9, 11, 11, false, 0.9, 170, true, 0, false, 0, false, ClientRtmStatus.Paused),
        new("Ravi", "Menon", 40, "F32.1", "Major depressive disorder, single episode, moderate", "Medicare Part B", "Strengthen behavioral activation.",
            0.55, 0.50, 0, 0, 13, 11, 10, 9, false, 1.2, 110, true, 19, true, 3, false, ClientRtmStatus.Active),
        new("Isabel", "Torres", 30, "F43.10", "Post-traumatic stress disorder, unspecified", "Medicaid MCO", "Build distress tolerance before trauma work.",
            0.55, 0.65, 0, 0, 13, 13, 14, 15, false, -0.6, 90, true, 22, true, 0, false, ClientRtmStatus.Active),
        new("Wesley", "Grant", 50, "F41.1", "Generalized anxiety disorder", "Medicare Part B", "Reduce worry time; improve focus at work.",
            0.70, 0.75, 0, 0, 7, 5, 14, 6, false, 1.6, 125, true, 20, true, 0, true, ClientRtmStatus.Active),
        new("Mei", "Chen", 28, "F32.0", "Major depressive disorder, single episode, mild", "Commercial PPO", "Reconnect with creativity and friends.",
            0.60, 0.35, 0, 0, 9, 6, 8, 6, false, 1.7, 85, true, 10, true, 0, false, ClientRtmStatus.Active),
    };

    private static readonly Persona[] ReyesClients =
    {
        new("Jamal", "Wright", 39, "F43.10", "Post-traumatic stress disorder, unspecified", "Medicaid MCO", "Reduce avoidance.",
            0.60, 0.70, 0, 0, 15, 12, 13, 11, false, 1.0, 100, true, 24, true, 0, false, ClientRtmStatus.Active),
        new("Lena", "Hoffman", 55, "F33.1", "Major depressive disorder, recurrent, moderate", "Medicare Part B", "Build social connection.",
            0.45, 0.30, -1, 0, 12, 14, 9, 10, false, 0.7, 140, true, 11, true, 0, false, ClientRtmStatus.Active),
        new("Carlos", "Mendes", 27, "F41.1", "Generalized anxiety disorder", "Commercial PPO", "Manage performance anxiety.",
            0.75, 0.50, 0, 0, 6, 3, 14, 4, false, 1.9, 160, true, 33, true, 0, true, ClientRtmStatus.Active),
        new("Ruth", "Abernathy", 68, "F43.23", "Adjustment disorder with mixed anxiety and depressed mood", "Medicare Part B", "Cope with bereavement.",
            0.50, 0.80, 0, 0, 14, 10, 10, 8, false, 1.3, 70, true, 18, true, 0, false, ClientRtmStatus.Active),
        new("Tess", "Delgado", 22, "F32.1", "Major depressive disorder, single episode, moderate", "Medicaid MCO", "Re-establish school routine.",
            0.35, 0.20, 0, 8, 16, 16, 12, 13, false, 0.5, 60, true, 5, false, 0, false, ClientRtmStatus.Active),
        new("Victor", "Osei", 46, "F41.0", "Panic disorder", "Commercial PPO", "Return to driving on highways.",
            0.65, 0.55, 1, 0, 8, 6, 15, 10, false, 1.5, 190, true, 40, true, 0, false, ClientRtmStatus.Active),
    };

    private static readonly Dictionary<string, string[]> TagsByExperience = new()
    {
        ["Creative Calm"] = new[] { "calm", "relaxed", "peaceful", "tired", "focused" },
        ["Explore Emotions"] = new[] { "sad", "anxious", "grief", "hopeful", "angry", "overwhelmed", "relieved" },
        ["Explore Self"] = new[] { "curious", "proud", "uncertain", "hopeful", "vulnerable" },
        ["Explore Journey"] = new[] { "hopeful", "grateful", "nostalgic", "determined", "tender" },
        ["Learn"] = new[] { "curious", "focused", "motivated" },
        ["Create"] = new[] { "playful", "proud", "calm", "inspired" },
        ["Reflect"] = new[] { "reflective", "sad", "relieved", "grateful", "anxious" },
        ["Getting Started"] = new[] { "curious", "focused", "uncertain" },
    };

    /// <summary>Clients who sometimes use Reflect between midnight and 5 AM (sleep and night-time worry).</summary>
    private static readonly HashSet<string> NightOwls = new() { "Elena Ruiz", "Omar Haddad" };

    /// <summary>XR users who never did the Training Tutorial.</summary>
    private static readonly HashSet<string> SkippedTutorial = new() { "Daniel Kim" };

    public static readonly Guid FacilityId = Guid.Parse("5b1c7a52-0d1e-4b8f-8f22-6a3d9c1e2f10");
    public const string FacilityName = "Riverbend Behavioral Health (Demo)";

    public static void Seed(InMemoryProviderDashboardStore store, DateTimeOffset nowUtc)
    {
        var today = EasternTime.Today(nowUtc);
        var facilityId = FacilityId;

        var ellison = new ProviderProfile
        {
            Id = StableGuid("provider-ellison"),
            UserId = DemoUsers.ProviderEllison.UserId, FullName = "Dr. Maya Ellison", Credentials = "PsyD, Licensed Clinical Psychologist",
            Email = DemoUsers.ProviderEllison.Email, Npi = "1999999901", FacilityId = facilityId, FacilityName = FacilityName
        };
        var reyes = new ProviderProfile
        {
            Id = StableGuid("provider-reyes"),
            UserId = DemoUsers.ProviderReyes.UserId, FullName = "Jordan Reyes", Credentials = "LCSW",
            Email = DemoUsers.ProviderReyes.Email, Npi = "1999999902", FacilityId = facilityId, FacilityName = FacilityName
        };
        store.Providers.Add(ellison);
        store.Providers.Add(reyes);

        var index = 0;
        foreach (var p in EllisonClients) SeedClient(store, ellison, p, index++, today, nowUtc);
        foreach (var p in ReyesClients) SeedClient(store, reyes, p, index++, today, nowUtc);
        SeedAuditHistory(store, ellison, reyes, today, nowUtc);
        SeedNotes(store, today, nowUtc);
    }

    /// <summary>A few provider notes so the Notes tab isn't empty on first run.</summary>
    private static void SeedNotes(InMemoryProviderDashboardStore store, DateOnly today, DateTimeOffset nowUtc)
    {
        var author = DemoUsers.ProviderEllison;
        void Note(string first, string last, int daysAgo, NoteTopic topic, string body, bool pinned = false)
        {
            var c = store.Clients.FirstOrDefault(x => x.FirstName == first && x.LastName == last);
            if (c == null) return;
            var at = EasternTime.AtLocal(today.AddDays(-daysAgo), new TimeOnly(15, 30));
            Guid? subject = null; var label = string.Empty;
            if (topic == NoteTopic.Session)
            {
                var s = store.Sessions.Where(x => x.ClientUserId == c.ClientUserId && x.StartedAt < at).OrderByDescending(x => x.StartedAt).FirstOrDefault();
                if (s != null) { subject = s.Id; label = $"{ModuleCatalog.Get(s.ModuleKey).Name}, {s.StartedAt.ToUsDateTimeString()}"; }
            }
            if (topic == NoteTopic.Assessment)
            {
                var a = store.Assessments.Where(x => x.ClientUserId == c.ClientUserId && x.CompletedAt < at).OrderByDescending(x => x.CompletedAt).FirstOrDefault();
                if (a != null) { subject = a.Id; label = $"{(a.Instrument == AssessmentInstrument.Phq9 ? "PHQ-9" : "GAD-7")} {a.TotalScore}, {a.CompletedAt.ToUsDateString()}"; }
            }
            store.Notes.Add(new ClientNote
            {
                ProviderClientId = c.Id, AuthorUserId = author.UserId, AuthorName = author.DisplayName, Body = body, Topic = topic,
                SubjectId = subject, SubjectLabel = label, IsPinned = pinned, DateCreated = at
            });
        }
        Note("Imani", "Brooks", 9, NoteTopic.General, "Goal for this month: use Self Compassion after hard days at work instead of scrolling. Agreed to try 3 times a week.", pinned: true);
        Note("Imani", "Brooks", 4, NoteTopic.Session, "Said this session helped her name the feeling as disappointment, not failure. Revisit next visit.");
        Note("Imani", "Brooks", 2, NoteTopic.Assessment, "Big drop since baseline. Credits evening routine and the Hope Tree module. Keep current plan.");
        Note("Elena", "Ruiz", 1, NoteTopic.Safety, "Safety plan reviewed by phone. Crisis line saved in phone, sister aware. Check in again within 48 hours.", pinned: true);
        Note("Elena", "Ruiz", 3, NoteTopic.General, "Reports poor sleep, often awake 1 to 3 AM. Using Floating Bubbles in bed. Discuss sleep hygiene.");
        Note("Daniel", "Kim", 2, NoteTopic.General, "New to VR. Felt dizzy first time. Show him the Training Tutorial at next visit.");
    }

    /// <summary>A plausible week of access events so the audit log has history on first run.</summary>
    private static void SeedAuditHistory(InMemoryProviderDashboardStore store, ProviderProfile ellison, ProviderProfile reyes, DateOnly today, DateTimeOffset nowUtc)
    {
        var rng = new Random(4242);
        string[] tabs = { "engagement", "response", "rtm", "art" };
        void Add(DateTimeOffset at, DemoUser u, AuditAction a, Guid? client = null, string detail = "", bool ok = true)
        {
            if (at >= nowUtc) return;
            store.Audit.Add(new AccessAuditEntry
            {
                OccurredAt = at, ActorUserId = a == AuditAction.SignInFailed ? null : u.UserId,
                ActorName = a == AuditAction.SignInFailed ? u.Email : u.DisplayName, ActorRole = a == AuditAction.SignInFailed ? string.Empty : u.Role,
                Action = a, ClientId = client, Detail = detail, Succeeded = ok
            });
        }

        var failedSeeded = false;
        for (var d = 6; d >= 1; d--)
        {
            var day = today.AddDays(-d);
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            foreach (var (prof, user) in new[] { (ellison, DemoUsers.ProviderEllison), (reyes, DemoUsers.ProviderReyes) })
            {
                var t = EasternTime.AtLocal(day, new TimeOnly(8 + rng.Next(0, 2), rng.Next(0, 60)));
                if (user == DemoUsers.ProviderReyes && d <= 3 && !failedSeeded)
                {
                    failedSeeded = true;
                    Add(t.AddMinutes(-2), user, AuditAction.SignInFailed, detail: "Wrong email or password", ok: false);
                }
                Add(t, user, AuditAction.SignIn);
                var mine = store.Clients.Where(c => c.ProviderId == prof.Id).OrderBy(_ => rng.Next()).Take(3 + rng.Next(0, 4)).ToList();
                foreach (var c in mine)
                {
                    t = t.AddMinutes(4 + rng.Next(0, 25));
                    Add(t, user, AuditAction.ViewClient, c.Id);
                    Add(t.AddMinutes(1), user, AuditAction.ViewClientTab, c.Id, tabs[rng.Next(tabs.Length)]);
                    if (rng.NextDouble() < 0.5) Add(t.AddMinutes(3), user, AuditAction.AttestReviewTime, c.Id);
                    if (rng.NextDouble() < 0.2) Add(t.AddMinutes(2), user, AuditAction.AskAssistant, c.Id);
                }
                Add(t.AddMinutes(30), user, AuditAction.SignOut);
            }
        }
        var admin = DemoUsers.FacilityAdmin;
        var at = EasternTime.AtLocal(today.AddDays(-2), new TimeOnly(15, 10));
        Add(at, admin, AuditAction.SignIn);
        Add(at.AddMinutes(1), admin, AuditAction.ViewBilling, detail: today.ToString("yyyy-MM"));
        Add(at.AddMinutes(4), admin, AuditAction.ExportReport, detail: today.AddMonths(-1).ToString("yyyy-MM"));
        Add(at.AddMinutes(9), admin, AuditAction.SignOut);
    }

    private static void SeedClient(InMemoryProviderDashboardStore store, ProviderProfile provider, Persona p, int index, DateOnly today, DateTimeOffset nowUtc)
    {
        var rng = new Random(2026 + index * 7919);
        var rtmStart = today.AddDays(-p.RtmStartDaysAgo);
        var client = new ProviderClient
        {
            Id = StableGuid($"client-{index}"),
            ProviderId = provider.Id,
            ClientUserId = StableGuid($"client-user-{index}"),
            FirstName = p.First,
            LastName = p.Last,
            DateOfBirth = today.AddYears(-p.Age).AddDays(-rng.Next(10, 340)),
            Mrn = $"RX-{100200 + index * 37}",
            PrimaryDiagnosisCode = p.DxCode,
            PrimaryDiagnosisLabel = p.DxLabel,
            PayerName = p.Payer,
            TreatmentGoals = p.Goals,
            RtmConsentDate = p.Consent ? rtmStart.AddDays(-1) : null,
            RtmStartDate = rtmStart,
            SetupBilled = p.RtmStartDaysAgo > 30,
            Status = p.Status,
            DateCreated = EasternTime.StartOfDayUtc(rtmStart.AddDays(-2))
        };
        store.Clients.Add(client);

        SeedSessions(store, client, p, rng, today, nowUtc);
        SeedAssessments(store, client, p, rng, today, nowUtc);
        SeedTimeEntries(store, provider, client, p, rng, today, nowUtc);
    }

    private static void SeedSessions(InMemoryProviderDashboardStore store, ProviderClient client, Persona p, Random rng, DateOnly today, DateTimeOffset nowUtc)
    {
        var first = new[] { today.AddDays(-120), client.RtmStartDate.AddDays(-5) }.Max();
        var period = RtmPeriods.DevicePeriodFor(client.RtmStartDate, today);

        HashSet<DateOnly> forced = null;
        if (p.ForcedPeriodDataDays.HasValue)
        {
            var candidates = Enumerable.Range(0, today.DayNumber - period.Start.DayNumber + 1).Select(i => period.Start.AddDays(i)).ToList();
            forced = candidates.OrderBy(_ => rng.Next()).Take(p.ForcedPeriodDataDays.Value).ToHashSet();
        }

        var xrModules = ModuleCatalog.All.Where(m => m.Platform == ReflectPlatform.ReflectXR && !m.IsTutorial).ToList();
        var mobileModules = ModuleCatalog.All.Where(m => m.Platform == ReflectPlatform.ReflectMobile).ToList();
        // Each client has a few favorite modules (high weights) so "who uses what most" has a clear shape.
        var weights = ModuleCatalog.All.ToDictionary(m => m.Key, _ => 0.2 + rng.NextDouble() * 0.8);
        foreach (var fav in xrModules.OrderBy(_ => rng.Next()).Take(2).Concat(mobileModules.OrderBy(_ => rng.Next()).Take(2)))
            weights[fav.Key] = 4 + rng.NextDouble() * 4;
        var fullName = $"{p.First} {p.Last}";
        var nightOwl = NightOwls.Contains(fullName);
        var tutorialDone = SkippedTutorial.Contains(fullName);

        for (var day = first; day <= today; day = day.AddDays(1))
        {
            var daysAgo = today.DayNumber - day.DayNumber;
            bool active;
            if (forced != null && period.Contains(day)) active = forced.Contains(day);
            else if (daysAgo < p.InactiveLastDays) active = false;
            else
            {
                var prob = p.ActiveDayProbability;
                if (daysAgo < 28 && p.EngagementTrend != 0)
                {
                    var t = (28 - daysAgo) / 28.0; // 0 → 1 over the last four weeks
                    prob *= p.EngagementTrend < 0 ? 1 - 0.75 * t : 1 + 0.35 * t;
                }
                active = rng.NextDouble() < Math.Min(0.95, prob);
            }
            if (!active) continue;

            var count = rng.NextDouble() < 0.25 ? 2 : 1;
            for (var i = 0; i < count; i++)
            {
                var isXr = rng.NextDouble() < p.XrShare;
                var lateNight = nightOwl && daysAgo < 21 && rng.NextDouble() < 0.5;
                if (lateNight) isXr = false; // phone in bed
                if (isXr && !tutorialDone)
                {
                    // First time in VR: the Training Tutorial comes first.
                    tutorialDone = true;
                    var tStart = EasternTime.AtLocal(day, new TimeOnly(rng.Next(16, 19), rng.Next(0, 60)));
                    if (tStart <= nowUtc.AddMinutes(-60))
                    {
                        store.Sessions.Add(new AppSession
                        {
                            ClientUserId = client.ClientUserId, Platform = ReflectPlatform.ReflectXR, ModuleKey = ModuleCatalog.TutorialKey,
                            StartedAt = tStart, EndedAt = tStart.AddMinutes(8 + rng.Next(0, 6)), Completed = true, DateCreated = tStart
                        });
                    }
                }
                var pool = isXr ? xrModules : mobileModules;
                var module = WeightedPick(pool, m => weights[m.Key], rng);
                var hour = lateNight ? rng.Next(0, 4) : isXr ? rng.Next(19, 22) : rng.Next(7, 23);
                var start = EasternTime.AtLocal(day, new TimeOnly(hour, rng.Next(0, 60)));
                if (start > nowUtc.AddMinutes(-40)) continue; // planned for later today: hasn't happened yet
                var minutes = isXr ? 10 + rng.Next(0, 26) : 4 + rng.Next(0, 17);
                var session = new AppSession
                {
                    ClientUserId = client.ClientUserId,
                    Platform = isXr ? ReflectPlatform.ReflectXR : ReflectPlatform.ReflectMobile,
                    ModuleKey = module.Key,
                    StartedAt = start,
                    EndedAt = start.AddMinutes(minutes).AddSeconds(rng.Next(0, 60)),
                    Completed = rng.NextDouble() < 0.85,
                    ArtworksCreated = module.Group is "Learn" ? 0 : rng.Next(0, 3),
                    ReflectionsWritten = rng.NextDouble() < 0.4 ? 1 : 0,
                    DateCreated = start
                };
                store.Sessions.Add(session);
                SeedCheckIns(store, client, p, session, module, rng, today, day);
                if (p.Wearable) SeedHeartRate(store, client, session, rng);
            }
        }
    }

    private static void SeedCheckIns(InMemoryProviderDashboardStore store, ProviderClient client, Persona p, AppSession session, ModuleInfo module, Random rng, DateOnly today, DateOnly day)
    {
        if (rng.NextDouble() > 0.8) return;
        var progress = 1 - Math.Clamp((today.DayNumber - day.DayNumber) / 120.0, 0, 1);
        var phqNow = p.PhqStart + (p.PhqEnd - p.PhqStart) * progress;
        var gadNow = p.GadStart + (p.GadEnd - p.GadStart) * progress;

        var preMood = Clamp((int)Math.Round(8.5 - phqNow / 3.5 + Noise(rng, 1.0)), 1, 10);
        var postMood = Clamp((int)Math.Round(preMood + p.MoodLift + Noise(rng, 0.9)), 1, 10);
        var preStress = Clamp((int)Math.Round(2.5 + gadNow / 2.6 + Noise(rng, 1.0)), 1, 10);
        var postStress = Clamp((int)Math.Round(preStress - p.MoodLift * 0.8 + Noise(rng, 0.9)), 1, 10);

        var tags = TagsByExperience.TryGetValue(module.Group, out var list) ? list : TagsByExperience["Reflect"];
        var chosen = tags.OrderBy(_ => rng.Next()).Take(rng.Next(1, 3));

        store.CheckIns.Add(new MoodCheckIn
        {
            ClientUserId = client.ClientUserId, AppSessionId = session.Id, Phase = CheckInPhase.PreSession,
            RecordedAt = session.StartedAt.AddSeconds(-30), MoodScore = preMood, StressScore = preStress
        });
        store.CheckIns.Add(new MoodCheckIn
        {
            ClientUserId = client.ClientUserId, AppSessionId = session.Id, Phase = CheckInPhase.PostSession,
            RecordedAt = session.EndedAt.AddSeconds(20), MoodScore = postMood, StressScore = postStress,
            EmotionTags = string.Join(",", chosen)
        });
    }

    private static void SeedHeartRate(InMemoryProviderDashboardStore store, ProviderClient client, AppSession session, Random rng)
    {
        var pre = 80 + rng.Next(0, 17);
        var post = pre - rng.Next(2, 11);
        store.Observations.Add(new Observation
        {
            ClientUserId = client.ClientUserId, Category = ObservationCategory.Physiologic, Code = ObservationCatalog.HeartRate,
            Value = pre, Unit = "beats/min", EffectiveAt = session.StartedAt.AddSeconds(-20), SourceDevice = "Paired wearable (synthetic)",
            AppSessionId = session.Id, Context = "pre"
        });
        store.Observations.Add(new Observation
        {
            ClientUserId = client.ClientUserId, Category = ObservationCategory.Physiologic, Code = ObservationCatalog.HeartRate,
            Value = post, Unit = "beats/min", EffectiveAt = session.EndedAt.AddSeconds(30), SourceDevice = "Paired wearable (synthetic)",
            AppSessionId = session.Id, Context = "post"
        });
    }

    private static void SeedAssessments(InMemoryProviderDashboardStore store, ProviderClient client, Persona p, Random rng, DateOnly today, DateTimeOffset nowUtc)
    {
        var first = new[] { today.AddDays(-120), client.RtmStartDate }.Max();
        var span = Math.Max(1, today.DayNumber - first.DayNumber);
        var dates = new List<DateOnly>();
        for (var d = first; d <= today; d = d.AddDays(14)) dates.Add(d);
        if (dates.Count == 0 || dates[^1].DayNumber < today.DayNumber - 4) dates.Add(today.AddDays(-1)); // latest check-in lands near today

        for (var i = 0; i < dates.Count; i++)
        {
            var progress = dates.Count == 1 ? 0 : (dates[i].DayNumber - first.DayNumber) / (double)span;
            var when = EasternTime.AtLocal(dates[i], new TimeOnly(rng.Next(8, 21), rng.Next(0, 60)));
            if (when > nowUtc) when = nowUtc.AddHours(-3);
            var isLast = i == dates.Count - 1;

            var phq = Clamp((int)Math.Round(p.PhqStart + (p.PhqEnd - p.PhqStart) * progress + (i == 0 || isLast ? 0 : Noise(rng, 1.0))), 0, 27);
            var gad = Clamp((int)Math.Round(p.GadStart + (p.GadEnd - p.GadStart) * progress + (i == 0 || isLast ? 0 : Noise(rng, 1.0))), 0, 21);

            store.Assessments.Add(new AssessmentResult
            {
                ClientUserId = client.ClientUserId, Instrument = AssessmentInstrument.Phq9, TotalScore = phq,
                SafetyItemScore = p.SafetyItem && isLast ? 1 : 0, CompletedAt = when
            });
            store.Assessments.Add(new AssessmentResult
            {
                ClientUserId = client.ClientUserId, Instrument = AssessmentInstrument.Gad7, TotalScore = gad, CompletedAt = when.AddMinutes(3)
            });
        }
    }

    private static void SeedTimeEntries(InMemoryProviderDashboardStore store, ProviderProfile provider, ProviderClient client, Persona p, Random rng, DateOnly today, DateTimeOffset nowUtc)
    {
        var month = RtmPeriods.CalendarMonthOf(today);
        var earliest = new[] { month.Start, client.RtmStartDate }.Max();

        // Current month: attested minutes split into blocks, plus an optional live interaction.
        var remaining = p.MonthMinutes;
        if (p.LiveCall && remaining > 0)
        {
            var live = Math.Min(remaining, 8 + rng.Next(0, 5));
            AddEntry(store, provider, client, RandomDay(earliest, today, rng), live, RtmActivityType.InteractiveCommunication, RtmTimeSource.Manual,
                rng.NextDouble() < 0.7 ? CommunicationMode.Video : CommunicationMode.Phone, "Reviewed Reflect trends with client; adjusted home-practice plan.", true, rng, nowUtc);
            remaining -= live;
        }
        while (remaining > 0)
        {
            var block = Math.Min(remaining, 3 + rng.Next(0, 7));
            AddEntry(store, provider, client, RandomDay(earliest, today, rng), block, RtmActivityType.DataReview, RtmTimeSource.AutoTracked,
                CommunicationMode.None, "Dashboard review.", true, rng, nowUtc);
            remaining -= block;
        }
        if (p.PendingMinutes > 0)
        {
            AddEntry(store, provider, client, today.AddDays(-1), p.PendingMinutes, RtmActivityType.DataReview, RtmTimeSource.AutoTracked,
                CommunicationMode.None, "Dashboard review.", false, rng, nowUtc);
        }

        // Two previous months of attested history for the report screen.
        for (var back = 1; back <= 2; back++)
        {
            var m = RtmPeriods.CalendarMonthOf(month.Start.AddMonths(-back));
            if (client.RtmStartDate > m.End) continue;
            var start = new[] { m.Start, client.RtmStartDate }.Max();
            var minutes = p.ActiveDayProbability < 0.3 ? rng.Next(0, 12) : 20 + rng.Next(0, 26);
            if (minutes >= 10)
            {
                AddEntry(store, provider, client, RandomDay(start, m.End, rng), 10, RtmActivityType.InteractiveCommunication, RtmTimeSource.Manual,
                    CommunicationMode.Video, "Monthly RTM check-in by video.", true, rng, nowUtc);
                minutes -= 10;
            }
            while (minutes > 0)
            {
                var block = Math.Min(minutes, 4 + rng.Next(0, 6));
                AddEntry(store, provider, client, RandomDay(start, m.End, rng), block, RtmActivityType.DataReview, RtmTimeSource.AutoTracked,
                    CommunicationMode.None, "Dashboard review.", true, rng, nowUtc);
                minutes -= block;
            }
        }
    }

    private static void AddEntry(InMemoryProviderDashboardStore store, ProviderProfile provider, ProviderClient client, DateOnly day, int minutes,
        RtmActivityType activity, RtmTimeSource source, CommunicationMode mode, string note, bool attested, Random rng, DateTimeOffset nowUtc)
    {
        var start = EasternTime.AtLocal(day, new TimeOnly(rng.Next(8, 17), rng.Next(0, 60)));
        if (start > nowUtc) start = nowUtc.AddHours(-1);
        store.TimeEntries.Add(new RtmTimeEntry
        {
            ProviderId = provider.Id, ClientId = client.Id, StartedAt = start, ActiveSeconds = minutes * 60,
            Activity = activity, Source = source, CommunicationMode = mode, Note = note,
            TrackingKey = source == RtmTimeSource.AutoTracked ? Guid.NewGuid().ToString("N") : string.Empty,
            LastHeartbeatAt = start.AddSeconds(minutes * 60), IsAttested = attested, AttestedAt = attested ? start.AddMinutes(minutes + 1) : null,
            DateCreated = start
        });
    }

    private static DateOnly RandomDay(DateOnly from, DateOnly to, Random rng) =>
        to < from ? from : from.AddDays(rng.Next(0, to.DayNumber - from.DayNumber + 1));

    private static T WeightedPick<T>(IReadOnlyList<T> items, Func<T, double> weight, Random rng)
    {
        var total = items.Sum(weight);
        var roll = rng.NextDouble() * total;
        foreach (var item in items)
        {
            roll -= weight(item);
            if (roll <= 0) return item;
        }
        return items[^1];
    }

    /// <summary>Deterministic ids so demo URLs survive restarts.</summary>
    private static Guid StableGuid(string name) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes("reflect-demo:" + name)));

    private static double Noise(Random rng, double scale) => (rng.NextDouble() + rng.NextDouble() + rng.NextDouble() - 1.5) * scale;
    private static int Clamp(int v, int lo, int hi) => Math.Max(lo, Math.Min(hi, v));
}
