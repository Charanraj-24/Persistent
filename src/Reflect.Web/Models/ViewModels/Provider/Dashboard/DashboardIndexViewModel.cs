using System.Globalization;
using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Catalogs;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Dashboard;

public class DashboardIndexViewModel
{
    public const int ChartWeeks = 12;

    public string ProviderFirstName { get; set; } = string.Empty;
    public string ProviderDisplayName { get; set; } = string.Empty;
    public string Greeting { get; set; } = string.Empty;
    public string TodayLabel { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public int PeriodDaysLeft { get; set; }

    public int ActivePatients { get; set; }
    public int TotalPatients { get; set; }
    public int SessionsLast7Days { get; set; }
    public int SessionsPrior7Days { get; set; }
    public int MinutesLast7Days { get; set; }
    public int ActivePatientsLast7Days { get; set; }
    public int MonitoringMinutesThisPeriod { get; set; }
    public int PatientsAtOrAbove20Minutes { get; set; }
    public int RtmEnrolledPatients { get; set; }
    public int NeedsAttentionCount { get; set; }

    public List<AttentionRowViewModel> AttentionRows { get; set; } = new();
    public List<RtmProgressRowViewModel> RtmRows { get; set; } = new();
    public List<RecentSessionRowViewModel> RecentSessions { get; set; } = new();
    public WeeklyChartData WeeklyChart { get; set; } = new();
    public ModuleChartData ModuleChart { get; set; } = new();

    public int? SessionsChangePercent => SessionsPrior7Days == 0 ? null : (int)Math.Round((SessionsLast7Days - SessionsPrior7Days) * 100.0 / SessionsPrior7Days);

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, CancellationToken ct)
    {
        var period = BillingPeriod.Containing(context.Today);
        var caseload = await CaseloadSnapshot.LoadAsync(repos, context, period, ct);
        var today = context.Today;
        var localNow = context.NowUtc.ToEastern();

        ProviderFirstName = caseload.Provider.FirstName;
        ProviderDisplayName = caseload.Provider.DisplayName;
        Greeting = localNow.Hour < 12 ? "Good morning" : localNow.Hour < 17 ? "Good afternoon" : "Good evening";
        TodayLabel = localNow.ToString("dddd, MMMM d, yyyy", CultureInfo.InvariantCulture);
        PeriodLabel = period.Label;
        PeriodDaysLeft = period.End.DayNumber - today.DayNumber + 1;

        var active = caseload.Patients.Where(p => p.Patient.Status == PatientStatus.Active).ToList();
        TotalPatients = caseload.Patients.Count;
        ActivePatients = active.Count;

        var last7 = caseload.AllSessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-7)).ToList();
        var prior7 = caseload.AllSessions.Where(s => s.StartedAt.ToEasternDate() <= today.AddDays(-7) && s.StartedAt.ToEasternDate() > today.AddDays(-14)).ToList();
        SessionsLast7Days = last7.Count;
        SessionsPrior7Days = prior7.Count;
        MinutesLast7Days = last7.Sum(s => s.DurationMinutes);
        ActivePatientsLast7Days = last7.Select(s => s.PatientId).Distinct().Count();

        var enrolled = caseload.Patients.Where(p => p.Rtm.Readiness != RtmReadiness.NotEnrolled && p.Patient.Status == PatientStatus.Active).ToList();
        RtmEnrolledPatients = enrolled.Count;
        MonitoringMinutesThisPeriod = caseload.MonitoringEntries.Sum(m => m.ActiveSeconds) / 60;
        PatientsAtOrAbove20Minutes = enrolled.Count(p => p.Rtm.TreatmentMinutes >= RtmBillingRules.FirstTreatmentBlockMinutes);

        AttentionRows = caseload.Patients
            .Where(p => p.TopSeverity.HasValue)
            .OrderByDescending(p => p.TopSeverity)
            .ThenByDescending(p => p.Attention.Count(a => a.Severity >= AttentionSeverity.Warning))
            .Select(AttentionRowViewModel.FromSnapshot)
            .ToList();
        NeedsAttentionCount = AttentionRows.Count;

        RtmRows = enrolled
            .OrderBy(p => p.Rtm.Readiness == RtmReadiness.Ready ? 1 : 0)
            .ThenBy(p => p.Rtm.TreatmentMinutes)
            .Select(RtmProgressRowViewModel.FromSnapshot)
            .ToList();

        var names = caseload.Patients.ToDictionary(p => p.Patient.Id, p => p.Patient);
        RecentSessions = caseload.AllSessions
            .OrderByDescending(s => s.StartedAt)
            .Take(8)
            .Select(s => RecentSessionRowViewModel.FromEntity(s, names[s.PatientId]))
            .ToList();

        var weekly = EngagementCalculator.Weekly(caseload.AllSessions, today, ChartWeeks);
        WeeklyChart = WeeklyChartData.From(weekly);

        var moduleUsage = EngagementCalculator.Summarize(caseload.AllSessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-30))).Modules;
        ModuleChart = ModuleChartData.From(moduleUsage);
    }
}

public class AttentionRowViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Concern { get; set; } = string.Empty;
    public StatusBadge Severity { get; set; }
    public List<AttentionItem> Items { get; set; } = new();

    public static AttentionRowViewModel FromSnapshot(PatientSnapshot s) => new()
    {
        PatientId = s.Patient.Id,
        PatientName = s.Patient.FullName,
        Initials = s.Patient.Initials,
        Concern = s.Patient.PrimaryConcern,
        Severity = StatusDisplay.Attention(s.TopSeverity ?? AttentionSeverity.Info),
        Items = s.Attention.Where(a => a.Severity >= AttentionSeverity.Warning).Take(2).ToList()
    };
}

public class RtmProgressRowViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public int DataDays { get; set; }
    public int DataDaysRequired { get; set; }
    public int TreatmentMinutes { get; set; }
    public int NextThreshold { get; set; }
    public bool HasInteractive { get; set; }
    public StatusBadge Status { get; set; }

    public int DataDaysPercent => Math.Min(100, DataDays * 100 / Math.Max(1, DataDaysRequired));
    public int MinutesPercent => Math.Min(100, TreatmentMinutes * 100 / RtmBillingRules.FirstTreatmentBlockMinutes);

    public static RtmProgressRowViewModel FromSnapshot(PatientSnapshot s) => new()
    {
        PatientId = s.Patient.Id,
        PatientName = s.Patient.FullName,
        Initials = s.Patient.Initials,
        DataDays = s.Rtm.DataDays,
        DataDaysRequired = s.Rtm.DataDaysRequired,
        TreatmentMinutes = s.Rtm.TreatmentMinutes,
        NextThreshold = s.Rtm.NextThresholdMinutes ?? 0,
        HasInteractive = s.Rtm.HasInteractiveCommunication,
        Status = StatusDisplay.Rtm(s.Rtm.Readiness)
    };
}

public class RecentSessionRowViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleIcon { get; set; } = string.Empty;
    public string PlatformName { get; set; } = string.Empty;
    public string PlatformIcon { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public string When { get; set; } = string.Empty;
    public int? MoodChange { get; set; }

    public static RecentSessionRowViewModel FromEntity(Domain.ProviderDashboard.Entities.ActivitySession s, Domain.ProviderDashboard.Entities.Patient patient)
    {
        var module = ModuleCatalog.Get(s.ModuleKey);
        return new RecentSessionRowViewModel
        {
            PatientId = patient.Id,
            PatientName = patient.FullName,
            Initials = patient.Initials,
            ModuleName = module.Name,
            ModuleIcon = module.Icon,
            PlatformName = ModuleCatalog.PlatformName(s.Platform),
            PlatformIcon = StatusDisplay.PlatformIcon(s.Platform),
            Minutes = s.DurationMinutes,
            When = s.StartedAt.ToUsDateTimeString(),
            MoodChange = s.MoodBefore.HasValue && s.MoodAfter.HasValue ? s.MoodAfter - s.MoodBefore : null
        };
    }
}

public class WeeklyChartData
{
    public List<string> Labels { get; set; } = new();
    public List<int> XrMinutes { get; set; } = new();
    public List<int> MobileMinutes { get; set; } = new();
    public List<int> Sessions { get; set; } = new();

    public static WeeklyChartData From(IReadOnlyList<WeeklyEngagement> weeks) => new()
    {
        Labels = weeks.Select(w => w.WeekStart.ToString("M/d", CultureInfo.InvariantCulture)).ToList(),
        XrMinutes = weeks.Select(w => w.XrMinutes).ToList(),
        MobileMinutes = weeks.Select(w => w.MobileMinutes).ToList(),
        Sessions = weeks.Select(w => w.Sessions).ToList()
    };
}

public class ModuleChartData
{
    public List<string> Labels { get; set; } = new();
    public List<int> Minutes { get; set; } = new();
    public List<int> Sessions { get; set; } = new();
    /// <summary>"xr" or "mobile" per bar, so bars are colored by platform (two series) rather than one hue per module.</summary>
    public List<string> Platforms { get; set; } = new();

    public static ModuleChartData From(IReadOnlyList<ModuleUsage> modules) => new()
    {
        // "Explore Emotions" exists on both platforms, so duplicate names get a platform suffix.
        Labels = modules.Select(m => modules.Count(o => o.ModuleName == m.ModuleName) > 1
            ? $"{m.ModuleName} ({(m.Platform == ReflectPlatform.ReflectXR ? "XR" : "Mobile")})"
            : m.ModuleName).ToList(),
        Minutes = modules.Select(m => m.Minutes).ToList(),
        Sessions = modules.Select(m => m.Sessions).ToList(),
        Platforms = modules.Select(m => m.Platform == ReflectPlatform.ReflectXR ? "xr" : "mobile").ToList()
    };
}
