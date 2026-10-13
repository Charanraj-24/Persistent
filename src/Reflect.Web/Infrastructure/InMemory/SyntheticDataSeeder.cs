using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Catalogs;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Standins;

namespace Reflect.Web.Infrastructure.InMemory;

/// <summary>
/// DEMO ONLY: builds a realistic, fully synthetic caseload relative to "now", so the dashboard always looks current.
/// Every name, chart number, and value is invented. Deterministic for a given day (fixed random seed).
/// Each persona exercises a different part of the dashboard (alerts, billing states, goals, RPM).
/// </summary>
public static class SyntheticDataSeeder
{
    private enum Persona
    {
        Improving,
        HighlyEngaged,
        Steady,
        Disengaging,
        Slowing,
        EngagementDrop,
        Worsening,
        Severe,
        NewStart,
        MoodDip,
        InClinicHeavy,
        NoConsent,
        Paused
    }

    private sealed record PatientSeed(
        string First, string Last, string Pronouns, int Age, string Concern, Persona Persona,
        bool Xr, bool Mobile, bool Device, int EnrolledDaysAgo, int PhqStart, int PhqEnd, int? MonitoringMinutesThisMonth);

    private static readonly PatientSeed[] RiveraCaseload =
    {
        new("Avery", "Thompson", "she/her", 29, "Generalized anxiety", Persona.Improving, true, true, true, 118, 17, 7, 24),
        new("Marcus", "Bell", "he/him", 41, "Major depressive disorder", Persona.HighlyEngaged, true, true, true, 112, 15, 6, 43),
        new("Priya", "Natarajan", "she/her", 34, "Adjustment disorder", Persona.Disengaging, false, true, false, 96, 11, 14, 6),
        new("Daniel", "Kim", "he/him", 52, "Major depressive disorder", Persona.Worsening, true, true, true, 104, 10, 17, 17),
        new("Sofia", "Alvarez", "she/her", 23, "Social anxiety", Persona.NewStart, true, true, false, 11, 13, 11, 8),
        new("Elijah", "Brooks", "he/him", 67, "Grief and loss", Persona.MoodDip, true, false, true, 88, 12, 11, 19),
        new("Hannah", "Weiss", "she/her", 31, "Social anxiety", Persona.Steady, false, true, false, 120, 9, 8, 21),
        new("Jamal", "Carter", "he/him", 38, "Post-traumatic stress", Persona.InClinicHeavy, true, false, true, 75, 16, 12, 12),
        new("Grace", "O'Neil", "she/her", 45, "Burnout and work stress", Persona.EngagementDrop, true, true, false, 99, 12, 12, 0),
        new("Mateo", "Rossi", "he/him", 27, "Panic disorder", Persona.Steady, true, true, true, 83, 11, 9, 27),
        new("Lily", "Chen", "she/her", 22, "Generalized anxiety", Persona.Improving, false, true, false, 90, 14, 6, 15),
        new("Noah", "Patel", "they/them", 36, "Insomnia and stress", Persona.Slowing, false, true, false, 70, 10, 11, 4),
        new("Zoe", "Martin", "she/her", 58, "Chronic pain coping", Persona.HighlyEngaged, true, false, true, 110, 14, 8, 62),
        new("Owen", "Harris", "he/him", 19, "Adjustment disorder", Persona.NoConsent, false, true, false, 40, 9, 8, null),
        new("Isabella", "Nguyen", "she/her", 33, "Major depressive disorder", Persona.Severe, true, true, false, 64, 16, 21, 11),
        new("Caleb", "Foster", "he/him", 48, "Alcohol use recovery support", Persona.Paused, false, true, false, 115, 8, 9, null)
    };

    private static readonly PatientSeed[] OkaforCaseload =
    {
        new("Ruby", "Jackson", "she/her", 26, "Generalized anxiety", Persona.Improving, true, true, true, 101, 15, 8, 22),
        new("Leo", "Wright", "he/him", 44, "Adjustment disorder", Persona.Steady, false, true, false, 87, 9, 8, 13),
        new("Aisha", "Rahman", "she/her", 30, "Major depressive disorder", Persona.Disengaging, true, true, false, 92, 13, 15, 3),
        new("Ethan", "Cole", "he/him", 55, "Caregiver stress", Persona.HighlyEngaged, true, false, true, 109, 12, 5, 34),
        new("Nora", "Lindqvist", "she/her", 39, "Perinatal mood concerns", Persona.NewStart, false, true, false, 9, 14, 13, 0),
        new("Samuel", "Ortiz", "he/him", 62, "Major depressive disorder", Persona.Worsening, true, true, false, 78, 11, 16, 18)
    };

    private static readonly string[] InteractiveSummaries =
    {
        "Reviewed this month's Reflect activity and mood ratings together; agreed to keep Creative Calm before bed.",
        "Discussed the recent drop in sessions; patient identified schedule changes and set a new practice time.",
        "Check-in on symptom changes since last visit; reviewed PHQ-9 trend and adjusted weekly goal.",
        "Talked through themes from recent Explore Emotions creations; plan to bring artwork to next session.",
        "Reviewed Explore Journey reflections; reinforced coping plan and confirmed next appointment.",
        "Follow-up on sleep routine; patient reports using the Learn lessons on grounding."
    };

    private static readonly string[] MessageSummaries =
    {
        "Sent a secure message encouraging use of Explore Self before Thursday's session.",
        "Patient messaged about a difficult week; replied with grounding resources and confirmed appointment.",
        "Shared a reminder about the weekly PHQ-9 check-in in Reflect Mobile.",
        "Responded to a question about how creations are shared with the care team."
    };

    private static readonly string[] ManualNotes =
    {
        "Reviewed session history and reflections before treatment-plan update.",
        "Reviewed mood and PHQ-9 trend; drafted plan adjustments.",
        "Compared VR and mobile engagement; prepared talking points for next session."
    };

    public static void Seed(InMemoryProviderDashboardStore store, DateTimeOffset nowUtc)
    {
        var today = nowUtc.ToEasternDate();
        var random = new Random(20261107);

        var providers = new[]
        {
            new Provider
            {
                Id = DemoUsers.All[0].UserId, FirstName = "Maya", LastName = "Rivera", Credentials = "PsyD", Title = "Licensed Clinical Psychologist",
                Email = DemoUsers.All[0].Email, FacilityName = "Harborview Behavioral Health (synthetic)", Npi = "0000000001"
            },
            new Provider
            {
                Id = DemoUsers.All[1].UserId, FirstName = "Jordan", LastName = "Okafor", Credentials = "LCSW", Title = "Licensed Clinical Social Worker",
                Email = DemoUsers.All[1].Email, FacilityName = "Harborview Behavioral Health (synthetic)", Npi = "0000000002"
            }
        };
        store.Providers.AddRange(providers);

        var chartNumber = 1040;
        foreach (var (provider, caseload) in new[] { (providers[0], RiveraCaseload), (providers[1], OkaforCaseload) })
        {
            foreach (var seed in caseload)
            {
                chartNumber += 7 + random.Next(1, 9);
                SeedPatient(store, provider, seed, chartNumber, today, nowUtc, random);
            }
        }
    }

    private static void SeedPatient(InMemoryProviderDashboardStore store, Provider provider, PatientSeed seed, int chartNumber,
        DateOnly today, DateTimeOffset nowUtc, Random random)
    {
        var enrolled = today.AddDays(-seed.EnrolledDaysAgo);
        DateOnly? consent = seed.Persona == Persona.NoConsent ? null : enrolled.AddDays(seed.Persona == Persona.NewStart ? 0 : random.Next(0, 6));
        var patient = new Patient
        {
            Id = DeterministicGuid(random),
            ProviderId = provider.Id,
            FirstName = seed.First,
            LastName = seed.Last,
            Pronouns = seed.Pronouns,
            ChartNumber = $"RX-{chartNumber:0000}",
            DateOfBirth = today.AddYears(-seed.Age).AddDays(-random.Next(10, 340)),
            PrimaryConcern = seed.Concern,
            Status = seed.Persona == Persona.Paused ? PatientStatus.Paused : PatientStatus.Active,
            EnrolledOn = enrolled,
            RtmConsentOn = consent,
            UsesReflectXR = seed.Xr,
            UsesReflectMobile = seed.Mobile,
            HasPhysiologicDevice = seed.Device,
            DateCreated = enrolled.StartOfEasternDayUtc()
        };
        store.Patients.Add(patient);

        var sessions = SeedSessions(store, patient, seed, today, nowUtc, random);
        var assessments = SeedAssessments(store, patient, seed, today, random);
        SeedGoals(store, patient, provider, seed, sessions, assessments, today, random);
        SeedCommunications(store, patient, provider, seed, today, nowUtc, random);
        SeedMonitoringTime(store, patient, provider, seed, today, nowUtc, random);
    }

    private static List<ActivitySession> SeedSessions(InMemoryProviderDashboardStore store, Patient patient, PatientSeed seed,
        DateOnly today, DateTimeOffset nowUtc, Random random)
    {
        var sessions = new List<ActivitySession>();
        var span = Math.Max(1, seed.EnrolledDaysAgo);

        for (var daysAgo = seed.EnrolledDaysAgo; daysAgo >= 0; daysAgo--)
        {
            var day = today.AddDays(-daysAgo);
            var progress = 1.0 - (double)daysAgo / span; // 0 at enrollment, 1 today
            var probability = DailyProbability(seed.Persona, daysAgo, progress);
            var count = random.NextDouble() < probability ? (random.NextDouble() < 0.18 ? 2 : 1) : 0;

            for (var i = 0; i < count; i++)
            {
                var platform = ChoosePlatform(seed, random);
                var module = ChooseModule(platform, seed.Persona, random);
                var minutes = platform == ReflectPlatform.ReflectXR ? random.Next(11, 36) : random.Next(4, 19);
                var startLocalHour = i == 0 ? random.Next(7, 15) : random.Next(17, 22);
                var start = day.StartOfEasternDayUtc().AddHours(startLocalHour).AddMinutes(random.Next(0, 60));
                if (start.AddMinutes(minutes) > nowUtc) continue;

                var setting = SessionSetting.Remote;
                if (platform == ReflectPlatform.ReflectXR)
                {
                    var inClinicChance = seed.Persona == Persona.InClinicHeavy ? 0.75 : 0.12;
                    if (random.NextDouble() < inClinicChance) setting = SessionSetting.InClinic;
                }

                var (before, after) = Mood(seed.Persona, platform, progress, daysAgo, random);
                var session = new ActivitySession
                {
                    Id = DeterministicGuid(random),
                    PatientId = patient.Id,
                    Platform = platform,
                    Setting = setting,
                    ModuleKey = module,
                    Pathway = module == "xr-explore-journey" ? (random.NextDouble() < 0.5 || seed.Persona == Persona.MoodDip ? ModuleCatalog.AppreciationForLife : ModuleCatalog.NewPathways) : string.Empty,
                    StartedAt = start,
                    EndedAt = start.AddMinutes(minutes).AddSeconds(random.Next(0, 59)),
                    MoodBefore = before,
                    MoodAfter = after,
                    CreationsCount = module is "mobile-create" ? random.Next(1, 5) : module.StartsWith("xr-") ? random.Next(0, 3) : 0,
                    ReflectionWritten = module == "mobile-reflect" || random.NextDouble() < 0.3,
                    Completed = random.NextDouble() < 0.92,
                    DateCreated = start
                };
                sessions.Add(session);

                if (seed.Device && platform == ReflectPlatform.ReflectXR)
                {
                    SeedReadings(store, patient, session, seed, random);
                }
            }
        }

        // Make the mood-dip pattern unmistakable: the last three rated sessions end lower than they started.
        if (seed.Persona == Persona.MoodDip)
        {
            foreach (var s in sessions.OrderByDescending(s => s.StartedAt).Take(3))
            {
                s.MoodBefore = random.Next(5, 8);
                s.MoodAfter = s.MoodBefore - random.Next(1, 3);
            }
        }

        store.Sessions.AddRange(sessions);
        return sessions;
    }

    private static double DailyProbability(Persona persona, int daysAgo, double progress) => persona switch
    {
        Persona.HighlyEngaged => 0.78,
        Persona.Steady => 0.58,
        Persona.Improving => 0.32 + 0.45 * progress,
        Persona.Disengaging => daysAgo > 18 ? 0.55 : 0.0,
        Persona.Slowing => daysAgo > 9 ? 0.55 : 0.0,
        Persona.EngagementDrop => daysAgo > 14 ? 0.72 : 0.18,
        Persona.Worsening => 0.6 - 0.25 * progress,
        Persona.Severe => daysAgo > 20 ? 0.5 : 0.3,
        Persona.NewStart => 0.72,
        Persona.MoodDip => 0.55,
        Persona.InClinicHeavy => 0.42,
        Persona.NoConsent => 0.45,
        Persona.Paused => daysAgo > 30 ? 0.35 : 0.0,
        _ => 0.5
    };

    private static ReflectPlatform ChoosePlatform(PatientSeed seed, Random random)
    {
        if (seed.Xr && !seed.Mobile) return ReflectPlatform.ReflectXR;
        if (!seed.Xr && seed.Mobile) return ReflectPlatform.ReflectMobile;
        return random.NextDouble() < 0.45 ? ReflectPlatform.ReflectXR : ReflectPlatform.ReflectMobile;
    }

    private static string ChooseModule(ReflectPlatform platform, Persona persona, Random random)
    {
        var roll = random.NextDouble();
        if (platform == ReflectPlatform.ReflectXR)
        {
            if (persona == Persona.MoodDip && roll < 0.45) return "xr-explore-journey";
            return roll switch
            {
                < 0.36 => "xr-creative-calm",
                < 0.60 => "xr-explore-emotions",
                < 0.80 => "xr-explore-self",
                _ => "xr-explore-journey"
            };
        }
        return roll switch
        {
            < 0.24 => "mobile-learn",
            < 0.58 => "mobile-create",
            < 0.84 => "mobile-reflect",
            _ => "mobile-explore-emotions"
        };
    }

    private static (int? before, int? after) Mood(Persona persona, ReflectPlatform platform, double progress, int daysAgo, Random random)
    {
        if (random.NextDouble() < 0.14) return (null, null);

        var baseline = persona switch
        {
            Persona.Improving or Persona.HighlyEngaged => 3.5 + 3.0 * progress,
            Persona.Worsening => 6.0 - 2.5 * progress,
            Persona.Severe => 3.5 - 1.0 * progress,
            Persona.Steady => 5.5,
            _ => 5.0
        };
        var before = Math.Clamp((int)Math.Round(baseline + random.NextDouble() * 2 - 1), 1, 10);
        var lift = platform == ReflectPlatform.ReflectXR ? 1.7 : 0.9;
        if (persona is Persona.Worsening or Persona.Severe) lift -= 0.6;
        var after = Math.Clamp((int)Math.Round(before + lift + random.NextDouble() * 2 - 1), 1, 10);
        return (before, after);
    }

    private static void SeedReadings(InMemoryProviderDashboardStore store, Patient patient, ActivitySession session, PatientSeed seed, Random random)
    {
        var resting = 64 + (seed.Age / 10) + random.Next(-3, 4);
        var startHr = resting + random.Next(8, 17);
        var calmDrop = session.ModuleKey == "xr-creative-calm" ? random.Next(6, 13) : random.Next(1, 8);
        var endHr = Math.Max(resting - 2, startHr - calmDrop);
        var startHrv = random.Next(26, 42);
        var endHrv = startHrv + (session.ModuleKey == "xr-creative-calm" ? random.Next(5, 13) : random.Next(0, 7));

        store.PhysiologicReadings.AddRange(new[]
        {
            Reading(patient, session, PhysiologicMetricCatalog.HeartRate, startHr, session.StartedAt, "start", random),
            Reading(patient, session, PhysiologicMetricCatalog.HeartRate, endHr, session.EndedAt, "end", random),
            Reading(patient, session, PhysiologicMetricCatalog.HeartRateVariability, startHrv, session.StartedAt, "start", random),
            Reading(patient, session, PhysiologicMetricCatalog.HeartRateVariability, endHrv, session.EndedAt, "end", random)
        });
    }

    private static PhysiologicReading Reading(Patient patient, ActivitySession session, string metric, decimal value, DateTimeOffset at, string context, Random random) => new()
    {
        Id = DeterministicGuid(random),
        PatientId = patient.Id,
        ActivitySessionId = session.Id,
        MetricCode = metric,
        Value = value,
        RecordedAt = at,
        Context = context,
        DeviceName = "Quest 3 + chest strap (synthetic)",
        DateCreated = at
    };

    private static List<Assessment> SeedAssessments(InMemoryProviderDashboardStore store, Patient patient, PatientSeed seed, DateOnly today, Random random)
    {
        var list = new List<Assessment>();
        var points = new List<int>();
        for (var daysAgo = seed.EnrolledDaysAgo; daysAgo >= 0; daysAgo -= 14) points.Add(daysAgo);
        if (seed.Persona is Persona.Disengaging or Persona.Slowing or Persona.Paused)
        {
            var cutoff = seed.Persona == Persona.Paused ? 30 : seed.Persona == Persona.Disengaging ? 18 : 9;
            points = points.Where(d => d > cutoff).ToList();
        }

        for (var i = 0; i < points.Count; i++)
        {
            var progress = points.Count == 1 ? 0 : (double)i / (points.Count - 1);
            var phq = (int)Math.Round(seed.PhqStart + (seed.PhqEnd - seed.PhqStart) * progress + (i == 0 || i == points.Count - 1 ? 0 : random.Next(-1, 2)));
            phq = Math.Clamp(phq, 0, 27);
            var gad = Math.Clamp((int)Math.Round(phq * 0.72 + random.Next(-1, 2)), 0, 21);
            var completedAt = today.AddDays(-points[i]).StartOfEasternDayUtc().AddHours(random.Next(12, 22));
            var platform = seed.Mobile ? ReflectPlatform.ReflectMobile : ReflectPlatform.ReflectXR;

            list.Add(new Assessment { Id = DeterministicGuid(random), PatientId = patient.Id, Instrument = AssessmentInstrument.Phq9, Score = phq, CompletedAt = completedAt, CompletedOn = platform, DateCreated = completedAt });
            list.Add(new Assessment { Id = DeterministicGuid(random), PatientId = patient.Id, Instrument = AssessmentInstrument.Gad7, Score = gad, CompletedAt = completedAt.AddMinutes(3), CompletedOn = platform, DateCreated = completedAt });
        }

        store.Assessments.AddRange(list);
        return list;
    }

    private static void SeedGoals(InMemoryProviderDashboardStore store, Patient patient, Provider provider, PatientSeed seed,
        List<ActivitySession> sessions, List<Assessment> assessments, DateOnly today, Random random)
    {
        var phq = assessments.Where(a => a.Instrument == AssessmentInstrument.Phq9).OrderBy(a => a.CompletedAt).ToList();
        var gad = assessments.Where(a => a.Instrument == AssessmentInstrument.Gad7).OrderBy(a => a.CompletedAt).ToList();
        var start = patient.EnrolledOn;

        if (phq.Count > 0)
        {
            store.Goals.Add(new TreatmentGoal
            {
                Id = DeterministicGuid(random), PatientId = patient.Id, ProviderId = provider.Id,
                Title = "Reduce depressive symptoms",
                Description = "Lower PHQ-9 into the mild range through regular creative practice and weekly sessions.",
                MeasureName = "PHQ-9 score", Unit = "points", Direction = GoalDirection.Decrease,
                BaselineValue = phq[0].Score, TargetValue = Math.Max(4, phq[0].Score - 8), CurrentValue = phq[^1].Score,
                StartDate = start, TargetDate = start.AddDays(150), DateCreated = start.StartOfEasternDayUtc()
            });
        }

        var recentWeeks = sessions.Count(s => s.StartedAt.ToEasternDate() > today.AddDays(-14)) / 2m;
        store.Goals.Add(new TreatmentGoal
        {
            Id = DeterministicGuid(random), PatientId = patient.Id, ProviderId = provider.Id,
            Title = "Build a consistent creative practice",
            Description = "Use Reflect XR or Reflect Mobile on most days, ideally at a planned time.",
            MeasureName = "Sessions per week (2-week average)", Unit = "sessions/week", Direction = GoalDirection.Increase,
            BaselineValue = 1, TargetValue = 4, CurrentValue = Math.Round(recentWeeks, 1),
            StartDate = start, TargetDate = start.AddDays(90), DateCreated = start.StartOfEasternDayUtc()
        });

        if (gad.Count > 0 && (seed.Concern.Contains("anxiety", StringComparison.OrdinalIgnoreCase) || seed.Concern.Contains("Panic") || seed.Concern.Contains("stress", StringComparison.OrdinalIgnoreCase)))
        {
            store.Goals.Add(new TreatmentGoal
            {
                Id = DeterministicGuid(random), PatientId = patient.Id, ProviderId = provider.Id,
                Title = "Use calming skills when anxious",
                Description = "Practice Creative Calm or a grounding lesson when anxiety rises; track GAD-7.",
                MeasureName = "GAD-7 score", Unit = "points", Direction = GoalDirection.Decrease,
                BaselineValue = gad[0].Score, TargetValue = Math.Max(4, gad[0].Score - 6), CurrentValue = gad[^1].Score,
                StartDate = start.AddDays(7), TargetDate = start.AddDays(120), DateCreated = start.AddDays(7).StartOfEasternDayUtc(),
                Status = seed.Persona == Persona.Paused ? GoalStatus.OnHold : GoalStatus.Active
            });
        }
    }

    private static void SeedCommunications(InMemoryProviderDashboardStore store, Patient patient, Provider provider, PatientSeed seed,
        DateOnly today, DateTimeOffset nowUtc, Random random)
    {
        var currentMonth = BillingPeriod.Containing(today);
        var month = BillingPeriod.Containing(patient.EnrolledOn);
        while (month.Start <= currentMonth.Start)
        {
            var isCurrent = month.Start == currentMonth.Start;
            var firstDay = month.Start < patient.EnrolledOn ? patient.EnrolledOn : month.Start;
            var lastDay = isCurrent ? today : month.End;
            if (firstDay <= lastDay)
            {
                // In the current month only some patients have had a video/phone contact yet, so the billing screen shows both states.
                var interactiveCount = isCurrent
                    ? (seed.Persona is Persona.HighlyEngaged or Persona.Improving or Persona.Worsening or Persona.Severe or Persona.NewStart or Persona.Steady ? 1 : 0)
                    : random.Next(1, 3);
                if (seed.Persona == Persona.Paused && isCurrent) interactiveCount = 0;
                if (seed.Last is "Rossi" or "Wright" && isCurrent) interactiveCount = 0;

                for (var i = 0; i < interactiveCount; i++)
                {
                    var at = RandomMoment(firstDay, lastDay, nowUtc, random);
                    store.Communications.Add(new CareCommunication
                    {
                        Id = DeterministicGuid(random), PatientId = patient.Id, ProviderId = provider.Id,
                        Channel = random.NextDouble() < 0.6 ? CommunicationChannel.VideoCall : CommunicationChannel.PhoneCall,
                        OccurredAt = at, DurationMinutes = random.Next(5, 16),
                        Summary = InteractiveSummaries[random.Next(InteractiveSummaries.Length)],
                        DiscussedRemoteData = true, DateCreated = at
                    });
                }

                for (var i = 0; i < random.Next(0, 3); i++)
                {
                    var at = RandomMoment(firstDay, lastDay, nowUtc, random);
                    store.Communications.Add(new CareCommunication
                    {
                        Id = DeterministicGuid(random), PatientId = patient.Id, ProviderId = provider.Id,
                        Channel = CommunicationChannel.SecureMessage, OccurredAt = at, DurationMinutes = random.Next(2, 6),
                        Summary = MessageSummaries[random.Next(MessageSummaries.Length)], DiscussedRemoteData = random.NextDouble() < 0.5, DateCreated = at
                    });
                }
            }
            month = month.Next();
        }
    }

    private static void SeedMonitoringTime(InMemoryProviderDashboardStore store, Patient patient, Provider provider, PatientSeed seed,
        DateOnly today, DateTimeOffset nowUtc, Random random)
    {
        if (patient.RtmConsentOn == null) return;
        var currentMonth = BillingPeriod.Containing(today);
        var month = BillingPeriod.Containing(patient.RtmConsentOn.Value);

        while (month.Start <= currentMonth.Start)
        {
            var isCurrent = month.Start == currentMonth.Start;
            var firstDay = month.Start < patient.RtmConsentOn.Value ? patient.RtmConsentOn.Value : month.Start;
            var lastDay = isCurrent ? today : month.End;
            var targetMinutes = isCurrent ? seed.MonitoringMinutesThisMonth ?? 0 : random.Next(14, 46);
            if (seed.Persona == Persona.Paused && !isCurrent) targetMinutes = random.Next(0, 12);

            var remainingSeconds = targetMinutes * 60 + (targetMinutes > 0 ? random.Next(0, 50) : 0);
            while (remainingSeconds > 0 && firstDay <= lastDay)
            {
                var chunk = Math.Min(remainingSeconds, random.Next(3, 10) * 60 + random.Next(0, 60));
                var start = RandomMoment(firstDay, lastDay, nowUtc.AddMinutes(-15), random);
                var manual = random.NextDouble() < 0.15;
                store.MonitoringEntries.Add(new MonitoringTimeEntry
                {
                    Id = DeterministicGuid(random), PatientId = patient.Id, ProviderId = provider.Id,
                    Source = manual ? MonitoringTimeSource.ManualEntry : MonitoringTimeSource.AutoTracked,
                    StartedAt = start, LastActivityAt = start.AddSeconds(chunk + random.Next(30, 240)), ActiveSeconds = chunk,
                    Note = manual ? ManualNotes[random.Next(ManualNotes.Length)] : string.Empty,
                    IsClosed = true, DateCreated = start
                });
                remainingSeconds -= chunk;
            }
            month = month.Next();
        }
    }

    private static DateTimeOffset RandomMoment(DateOnly firstDay, DateOnly lastDay, DateTimeOffset notAfterUtc, Random random)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var day = firstDay.AddDays(random.Next(0, lastDay.DayNumber - firstDay.DayNumber + 1));
            var at = day.StartOfEasternDayUtc().AddHours(random.Next(8, 18)).AddMinutes(random.Next(0, 60));
            if (at <= notAfterUtc) return at;
        }
        return firstDay.StartOfEasternDayUtc().AddHours(8) <= notAfterUtc ? firstDay.StartOfEasternDayUtc().AddHours(8) : notAfterUtc.AddMinutes(-30);
    }

    private static Guid DeterministicGuid(Random random)
    {
        var bytes = new byte[16];
        random.NextBytes(bytes);
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x40);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
