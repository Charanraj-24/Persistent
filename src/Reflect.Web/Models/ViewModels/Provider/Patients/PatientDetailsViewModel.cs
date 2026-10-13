using System.Globalization;
using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Catalogs;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.Provider.Dashboard;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Patients;

public class PatientDetailsViewModel
{
    public const int ChartWeeks = 12;
    public const int MoodChartSessions = 20;

    // Header
    public Guid PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ChartNumber { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Pronouns { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Concern { get; set; } = string.Empty;
    public StatusBadge Status { get; set; }
    public string EnrolledOn { get; set; } = string.Empty;
    public string RtmConsentOn { get; set; } = string.Empty;
    public bool UsesXr { get; set; }
    public bool UsesMobile { get; set; }
    public bool HasPhysiologicDevice { get; set; }

    // Overview
    public EngagementSummary Last30Days { get; set; }
    public string LastActiveLabel { get; set; } = string.Empty;
    public int? LatestPhq9 { get; set; }
    public StatusBadge LatestPhq9Band { get; set; }
    public int? LatestGad7 { get; set; }
    public StatusBadge LatestGad7Band { get; set; }
    public List<Insight> Insights { get; set; } = new();
    public List<AttentionItem> Attention { get; set; } = new();
    public WeeklyChartData WeeklyChart { get; set; } = new();
    public ModuleChartData ModuleChart { get; set; } = new();
    public MoodChartData MoodChart { get; set; } = new();
    public AssessmentChartData AssessmentChart { get; set; } = new();

    // Tabs
    public List<SessionRowViewModel> Sessions { get; set; } = new();
    public List<GoalCardViewModel> Goals { get; set; } = new();
    public List<AssessmentRowViewModel> AssessmentRows { get; set; } = new();
    public List<CommunicationRowViewModel> Communications { get; set; } = new();
    public RtmPanelViewModel Rtm { get; set; } = new();
    public PhysiologicPanelViewModel Physiologic { get; set; } = new();

    public string ActiveTab { get; set; } = "overview";

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, Guid patientId, CancellationToken ct, string tab = "")
    {
        var patient = await repos.PatientRepo.GetForProviderAsync(patientId, context.ProviderId, ct)
            ?? throw new EntityNotFoundException(ProviderDashboardMessages.PatientNotFound);

        var today = context.Today;
        var period = BillingPeriod.Containing(today);
        var historyStart = today.AddDays(-CaseloadSnapshot.HistoryDays).StartOfEasternDayUtc();
        var until = context.NowUtc.AddMinutes(1);

        var sessions = (await repos.ActivitySessionRepo.GetByPatientAsync(patient.Id, historyStart, until, ct)).OrderByDescending(s => s.StartedAt).ToList();
        var assessments = (await repos.AssessmentRepo.GetByPatientAsync(patient.Id, ct)).OrderBy(a => a.CompletedAt).ToList();
        var goals = await repos.TreatmentGoalRepo.GetByPatientAsync(patient.Id, ct);
        var communications = (await repos.CareCommunicationRepo.GetByPatientAsync(patient.Id, ct)).OrderByDescending(c => c.OccurredAt).ToList();
        var monitoring = await repos.MonitoringTimeEntryRepo.GetByPatientAsync(patient.Id, period.StartUtc, period.EndUtcExclusive, ct);
        var readings = await repos.PhysiologicReadingRepo.GetByPatientAsync(patient.Id, historyStart, until, ct);

        PatientId = patient.Id;
        FullName = patient.FullName;
        Initials = patient.Initials;
        ChartNumber = patient.ChartNumber;
        Age = patient.AgeOn(today);
        Pronouns = patient.Pronouns;
        DateOfBirth = patient.DateOfBirth.ToUsDateString();
        Concern = patient.PrimaryConcern;
        Status = StatusDisplay.Patient(patient.Status);
        EnrolledOn = patient.EnrolledOn.ToUsDateString();
        RtmConsentOn = patient.RtmConsentOn?.ToUsDateString() ?? string.Empty;
        UsesXr = patient.UsesReflectXR;
        UsesMobile = patient.UsesReflectMobile;
        HasPhysiologicDevice = patient.HasPhysiologicDevice;

        Last30Days = EngagementCalculator.Summarize(sessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-30)));
        LastActiveLabel = StatusDisplay.RelativeDays(EngagementCalculator.DaysSinceLastSession(sessions, today));

        var phq = assessments.Where(a => a.Instrument == AssessmentInstrument.Phq9).ToList();
        var gad = assessments.Where(a => a.Instrument == AssessmentInstrument.Gad7).ToList();
        if (phq.Count > 0)
        {
            LatestPhq9 = phq[^1].Score;
            var band = AssessmentSeverity.Classify(AssessmentInstrument.Phq9, phq[^1].Score);
            LatestPhq9Band = StatusDisplay.Severity(band.Level, band.Label);
        }
        if (gad.Count > 0)
        {
            LatestGad7 = gad[^1].Score;
            var band = AssessmentSeverity.Classify(AssessmentInstrument.Gad7, gad[^1].Score);
            LatestGad7Band = StatusDisplay.Severity(band.Level, band.Label);
        }

        var rtm = CaseloadSnapshot.CalculateRtm(patient, sessions, communications, monitoring, period, today);
        Insights = InsightBuilder.Build(sessions, assessments, readings, today).ToList();
        Attention = patient.Status == PatientStatus.Active
            ? AttentionRules.Evaluate(sessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-60)).ToList(), assessments, rtm, today).ToList()
            : new List<AttentionItem>();

        WeeklyChart = WeeklyChartData.From(EngagementCalculator.Weekly(sessions, today, ChartWeeks));
        ModuleChart = ModuleChartData.From(EngagementCalculator.Summarize(sessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-90))).Modules);
        MoodChart = MoodChartData.From(sessions);
        AssessmentChart = AssessmentChartData.From(assessments);

        Sessions = sessions.Select(SessionRowViewModel.FromEntity).ToList();
        Goals = goals.OrderBy(g => g.Status).ThenBy(g => g.TargetDate).Select(g => GoalCardViewModel.FromEntity(g, today)).ToList();
        AssessmentRows = assessments.OrderByDescending(a => a.CompletedAt).Select(AssessmentRowViewModel.FromEntity).ToList();
        Communications = communications.Select(CommunicationRowViewModel.FromEntity).ToList();
        Rtm = RtmPanelViewModel.Build(rtm, period, today, sessions, monitoring);
        Physiologic = PhysiologicPanelViewModel.Build(patient, readings, sessions);

        ActiveTab = tab is "sessions" or "goals" or "communications" or "monitoring" or "physiologic" ? tab : "overview";
    }
}

public class SessionRowViewModel
{
    public string DateTime { get; set; } = string.Empty;
    public long SortKey { get; set; }
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformIcon { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string Pathway { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public StatusBadge Setting { get; set; }
    public int? MoodBefore { get; set; }
    public int? MoodAfter { get; set; }
    public int Creations { get; set; }
    public bool Reflection { get; set; }
    public bool Completed { get; set; }

    public static SessionRowViewModel FromEntity(ActivitySession s) => new()
    {
        DateTime = s.StartedAt.ToUsDateTimeString(),
        SortKey = s.StartedAt.ToUnixTimeSeconds(),
        PlatformName = ModuleCatalog.PlatformName(s.Platform),
        PlatformIcon = StatusDisplay.PlatformIcon(s.Platform),
        ModuleName = ModuleCatalog.Get(s.ModuleKey).Name,
        Pathway = s.Pathway,
        Minutes = s.DurationMinutes,
        Setting = StatusDisplay.Setting(s.Setting),
        MoodBefore = s.MoodBefore,
        MoodAfter = s.MoodAfter,
        Creations = s.CreationsCount,
        Reflection = s.ReflectionWritten,
        Completed = s.Completed
    };
}

public class GoalCardViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MeasureName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Baseline { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Current { get; set; } = string.Empty;
    public string DirectionLabel { get; set; } = string.Empty;
    public int Percent { get; set; }
    public int ExpectedPercent { get; set; }
    public StatusBadge Track { get; set; }
    public string TargetDate { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;

    public static GoalCardViewModel FromEntity(TreatmentGoal g, DateOnly today)
    {
        var progress = GoalProgressCalculator.Calculate(g, today);
        return new GoalCardViewModel
        {
            Id = g.Id,
            Title = g.Title,
            Description = g.Description,
            MeasureName = g.MeasureName,
            Unit = g.Unit,
            Baseline = Format(g.BaselineValue),
            Target = Format(g.TargetValue),
            Current = Format(g.CurrentValue),
            DirectionLabel = g.Direction == GoalDirection.Decrease ? "Lower is better" : "Higher is better",
            Percent = progress.Percent,
            ExpectedPercent = progress.ExpectedPercent,
            Track = StatusDisplay.Goal(progress.Track),
            TargetDate = g.TargetDate.ToUsDateString(),
            StartDate = g.StartDate.ToUsDateString()
        };
    }

    private static string Format(decimal value) => value.ToString("0.#", CultureInfo.InvariantCulture);
}

public class AssessmentRowViewModel
{
    public string Date { get; set; } = string.Empty;
    public long SortKey { get; set; }
    public string Instrument { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public StatusBadge Band { get; set; }
    public string CompletedOn { get; set; } = string.Empty;

    public static AssessmentRowViewModel FromEntity(Assessment a)
    {
        var band = AssessmentSeverity.Classify(a.Instrument, a.Score);
        return new AssessmentRowViewModel
        {
            Date = a.CompletedAt.ToUsDateString(),
            SortKey = a.CompletedAt.ToUnixTimeSeconds(),
            Instrument = AssessmentSeverity.Name(a.Instrument),
            Score = a.Score,
            MaxScore = AssessmentSeverity.MaxScore(a.Instrument),
            Band = StatusDisplay.Severity(band.Level, band.Label),
            CompletedOn = a.CompletedOn.HasValue ? ModuleCatalog.PlatformName(a.CompletedOn.Value) : "In clinic"
        };
    }
}

public class CommunicationRowViewModel
{
    public string When { get; set; } = string.Empty;
    public long SortKey { get; set; }
    public StatusBadge Channel { get; set; }
    public bool IsInteractive { get; set; }
    public int Minutes { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool DiscussedRemoteData { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid PatientId { get; set; }

    public static CommunicationRowViewModel FromEntity(CareCommunication c) => new()
    {
        When = c.OccurredAt.ToUsDateTimeString(),
        SortKey = c.OccurredAt.ToUnixTimeSeconds(),
        Channel = StatusDisplay.Channel(c.Channel),
        IsInteractive = c.IsInteractive,
        Minutes = c.DurationMinutes,
        Summary = c.Summary,
        DiscussedRemoteData = c.DiscussedRemoteData,
        PatientId = c.PatientId
    };
}

public class MoodChartData
{
    public List<string> Labels { get; set; } = new();
    public List<int> Before { get; set; } = new();
    public List<int> After { get; set; } = new();

    public static MoodChartData From(IEnumerable<ActivitySession> sessions)
    {
        var rated = sessions
            .Where(s => s.MoodBefore.HasValue && s.MoodAfter.HasValue)
            .OrderByDescending(s => s.StartedAt)
            .Take(PatientDetailsViewModel.MoodChartSessions)
            .OrderBy(s => s.StartedAt)
            .ToList();
        return new MoodChartData
        {
            Labels = rated.Select(s => s.StartedAt.ToEastern().ToString("M/d", CultureInfo.InvariantCulture)).ToList(),
            Before = rated.Select(s => s.MoodBefore.Value).ToList(),
            After = rated.Select(s => s.MoodAfter.Value).ToList()
        };
    }
}

public class AssessmentChartData
{
    public List<string> Labels { get; set; } = new();
    public List<int?> Phq9 { get; set; } = new();
    public List<int?> Gad7 { get; set; } = new();

    public static AssessmentChartData From(IReadOnlyList<Assessment> assessments)
    {
        var byDay = assessments.GroupBy(a => a.CompletedAt.ToEasternDate()).OrderBy(g => g.Key).ToList();
        return new AssessmentChartData
        {
            Labels = byDay.Select(g => g.Key.ToString("M/d", CultureInfo.InvariantCulture)).ToList(),
            Phq9 = byDay.Select(g => g.FirstOrDefault(a => a.Instrument == AssessmentInstrument.Phq9)?.Score).ToList(),
            Gad7 = byDay.Select(g => g.FirstOrDefault(a => a.Instrument == AssessmentInstrument.Gad7)?.Score).ToList()
        };
    }
}

public class CalendarDayViewModel
{
    public int Day { get; set; }
    public string Label { get; set; } = string.Empty;
    /// <summary>"data" (remote session), "clinic" (in-clinic only), "none", or "future".</summary>
    public string State { get; set; } = string.Empty;
    public bool IsToday { get; set; }
}

public class RtmPanelViewModel
{
    public string PeriodLabel { get; set; } = string.Empty;
    public RtmBillingSummary Summary { get; set; }
    public StatusBadge Readiness { get; set; }
    public int LeadingBlankDays { get; set; }
    public List<CalendarDayViewModel> Days { get; set; } = new();
    public int MonitoringSeconds { get; set; }
    public List<MonitoringEntryRowViewModel> Entries { get; set; } = new();
    public int MeterMaxMinutes { get; set; }

    public static RtmPanelViewModel Build(RtmBillingSummary summary, BillingPeriod period, DateOnly today,
        IEnumerable<ActivitySession> sessions, IEnumerable<MonitoringTimeEntry> monitoring)
    {
        var inPeriod = sessions.Where(s => period.Contains(s.StartedAt.ToEasternDate())).ToList();
        var remoteDays = inPeriod.Where(s => s.Setting == SessionSetting.Remote).Select(s => s.StartedAt.ToEasternDate()).ToHashSet();
        var clinicDays = inPeriod.Where(s => s.Setting == SessionSetting.InClinic).Select(s => s.StartedAt.ToEasternDate()).ToHashSet();

        var days = new List<CalendarDayViewModel>();
        for (var d = period.Start; d <= period.End; d = d.AddDays(1))
        {
            var state = d > today ? "future" : remoteDays.Contains(d) ? "data" : clinicDays.Contains(d) ? "clinic" : "none";
            var stateText = state switch
            {
                "data" => "remote data day",
                "clinic" => "in-clinic only, not a data day",
                "future" => "upcoming",
                _ => "no activity"
            };
            days.Add(new CalendarDayViewModel
            {
                Day = d.Day,
                State = state,
                IsToday = d == today,
                Label = $"{d.ToString("MMMM d", CultureInfo.InvariantCulture)}: {stateText}"
            });
        }

        var entries = monitoring.Where(m => m.ActiveSeconds > 0).OrderByDescending(m => m.StartedAt).Select(MonitoringEntryRowViewModel.FromEntity).ToList();
        return new RtmPanelViewModel
        {
            PeriodLabel = period.Label,
            Summary = summary,
            Readiness = StatusDisplay.Rtm(summary.Readiness),
            LeadingBlankDays = ((int)period.Start.DayOfWeek + 6) % 7,
            Days = days,
            MonitoringSeconds = monitoring.Sum(m => m.ActiveSeconds),
            Entries = entries,
            MeterMaxMinutes = Math.Max(60, ((summary.TreatmentMinutes / 20) + 1) * 20)
        };
    }
}

public class MonitoringEntryRowViewModel
{
    public string When { get; set; } = string.Empty;
    public long SortKey { get; set; }
    public string Duration { get; set; } = string.Empty;
    public int Seconds { get; set; }
    public StatusBadge Source { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool InProgress { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public Guid PatientId { get; set; }

    public static MonitoringEntryRowViewModel FromEntity(MonitoringTimeEntry m) => new()
    {
        When = m.StartedAt.ToUsDateTimeString(),
        SortKey = m.StartedAt.ToUnixTimeSeconds(),
        Duration = StatusDisplay.FormatDuration(m.ActiveSeconds),
        Seconds = m.ActiveSeconds,
        Source = StatusDisplay.Source(m.Source),
        Note = m.Note,
        InProgress = !m.IsClosed,
        PatientId = m.PatientId
    };
}

public class PhysiologicPanelViewModel
{
    public bool HasData { get; set; }
    public List<PhysiologicMetric> SupportedMetrics { get; set; } = new();
    public List<string> Labels { get; set; } = new();
    public List<decimal> HeartRateStart { get; set; } = new();
    public List<decimal> HeartRateEnd { get; set; } = new();
    public List<decimal> HrvStart { get; set; } = new();
    public List<decimal> HrvEnd { get; set; } = new();
    public string DeviceName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public decimal AverageStartHeartRate { get; set; }
    public decimal AverageEndHeartRate { get; set; }
    public decimal AverageHrvChange { get; set; }
    public string LastReading { get; set; } = string.Empty;

    public static PhysiologicPanelViewModel Build(Patient patient, IReadOnlyList<PhysiologicReading> readings, IReadOnlyList<ActivitySession> sessions)
    {
        var vm = new PhysiologicPanelViewModel { SupportedMetrics = PhysiologicMetricCatalog.All.ToList() };
        if (readings.Count == 0) return vm;

        var sessionStarts = sessions.ToDictionary(s => s.Id, s => s.StartedAt);
        var perSession = readings
            .Where(r => r.ActivitySessionId.HasValue && sessionStarts.ContainsKey(r.ActivitySessionId.Value))
            .GroupBy(r => r.ActivitySessionId.Value)
            .Select(g => new
            {
                Start = sessionStarts[g.Key],
                HrStart = g.FirstOrDefault(r => r.MetricCode == PhysiologicMetricCatalog.HeartRate && r.Context == "start")?.Value,
                HrEnd = g.FirstOrDefault(r => r.MetricCode == PhysiologicMetricCatalog.HeartRate && r.Context == "end")?.Value,
                HrvStart = g.FirstOrDefault(r => r.MetricCode == PhysiologicMetricCatalog.HeartRateVariability && r.Context == "start")?.Value,
                HrvEnd = g.FirstOrDefault(r => r.MetricCode == PhysiologicMetricCatalog.HeartRateVariability && r.Context == "end")?.Value
            })
            .Where(x => x.HrStart.HasValue && x.HrEnd.HasValue)
            .OrderBy(x => x.Start)
            .TakeLast(24)
            .ToList();
        if (perSession.Count == 0) return vm;

        vm.HasData = true;
        vm.DeviceName = readings.OrderByDescending(r => r.RecordedAt).First().DeviceName;
        vm.LastReading = readings.Max(r => r.RecordedAt).ToUsDateTimeString();
        vm.SessionCount = perSession.Count;
        vm.Labels = perSession.Select(x => x.Start.ToEastern().ToString("M/d", CultureInfo.InvariantCulture)).ToList();
        vm.HeartRateStart = perSession.Select(x => x.HrStart.Value).ToList();
        vm.HeartRateEnd = perSession.Select(x => x.HrEnd.Value).ToList();
        vm.HrvStart = perSession.Select(x => x.HrvStart ?? 0).ToList();
        vm.HrvEnd = perSession.Select(x => x.HrvEnd ?? 0).ToList();
        vm.AverageStartHeartRate = Math.Round(vm.HeartRateStart.Average(), 0);
        vm.AverageEndHeartRate = Math.Round(vm.HeartRateEnd.Average(), 0);
        vm.AverageHrvChange = Math.Round(perSession.Where(x => x.HrvStart.HasValue && x.HrvEnd.HasValue).Select(x => x.HrvEnd.Value - x.HrvStart.Value).DefaultIfEmpty(0).Average(), 1);
        return vm;
    }
}
