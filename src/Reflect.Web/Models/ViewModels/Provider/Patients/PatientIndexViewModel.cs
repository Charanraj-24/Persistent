using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Patients;

public class PatientIndexViewModel
{
    public const string FilterAttention = "attention";
    public const string FilterRtm = "rtm";
    public const string FilterInactive = "inactive";

    public string Filter { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int AttentionCount { get; set; }
    public int RtmFollowUpCount { get; set; }
    public int InactiveCount { get; set; }
    public List<PatientRowViewModel> Rows { get; set; } = new();

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, string filter, CancellationToken ct)
    {
        var caseload = await CaseloadSnapshot.LoadAsync(repos, context, BillingPeriod.Containing(context.Today), ct);
        var all = caseload.Patients.Select(s => PatientRowViewModel.FromSnapshot(s, context.Today)).ToList();

        TotalCount = all.Count;
        AttentionCount = all.Count(r => r.NeedsAttention);
        RtmFollowUpCount = all.Count(r => r.RtmNeedsFollowUp);
        InactiveCount = all.Count(r => r.DaysSinceLastSession >= AttentionRules.InactiveWarningDays);

        Filter = filter is FilterAttention or FilterRtm or FilterInactive ? filter : string.Empty;
        Rows = Filter switch
        {
            FilterAttention => all.Where(r => r.NeedsAttention).ToList(),
            FilterRtm => all.Where(r => r.RtmNeedsFollowUp).ToList(),
            FilterInactive => all.Where(r => r.DaysSinceLastSession >= AttentionRules.InactiveWarningDays).ToList(),
            _ => all
        };
    }
}

public class PatientRowViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ChartNumber { get; set; } = string.Empty;
    public string AgeAndPronouns { get; set; } = string.Empty;
    public string Concern { get; set; } = string.Empty;
    public StatusBadge Status { get; set; }
    public bool UsesXr { get; set; }
    public bool UsesMobile { get; set; }
    public int DaysSinceLastSession { get; set; }
    public string LastActiveLabel { get; set; } = string.Empty;
    public int Sessions30 { get; set; }
    public int Minutes30 { get; set; }
    public int? Phq9 { get; set; }
    public int? Phq9Change { get; set; }
    public StatusBadge Phq9Band { get; set; }
    public StatusBadge Rtm { get; set; }
    public int RtmDataDays { get; set; }
    public int RtmMinutes { get; set; }
    public StatusBadge Attention { get; set; }
    public string AttentionSummary { get; set; } = string.Empty;
    public bool NeedsAttention { get; set; }
    public bool RtmNeedsFollowUp { get; set; }

    public static PatientRowViewModel FromSnapshot(PatientSnapshot s, DateOnly today)
    {
        var p = s.Patient;
        var band = s.LatestPhq9 == null ? null : AssessmentSeverity.Classify(AssessmentInstrument.Phq9, s.LatestPhq9.Score);
        var top = s.TopSeverity;
        return new PatientRowViewModel
        {
            Id = p.Id,
            Name = p.FullName,
            Initials = p.Initials,
            ChartNumber = p.ChartNumber,
            AgeAndPronouns = string.IsNullOrEmpty(p.Pronouns) ? $"{p.AgeOn(today)}" : $"{p.AgeOn(today)} · {p.Pronouns}",
            Concern = p.PrimaryConcern,
            Status = StatusDisplay.Patient(p.Status),
            UsesXr = p.UsesReflectXR,
            UsesMobile = p.UsesReflectMobile,
            DaysSinceLastSession = s.DaysSinceLastSession,
            LastActiveLabel = StatusDisplay.RelativeDays(s.DaysSinceLastSession),
            Sessions30 = s.Last30Days.Sessions,
            Minutes30 = s.Last30Days.TotalMinutes,
            Phq9 = s.LatestPhq9?.Score,
            Phq9Change = s.Phq9Change,
            Phq9Band = band == null ? null : StatusDisplay.Severity(band.Level, band.Label),
            Rtm = StatusDisplay.Rtm(s.Rtm.Readiness),
            RtmDataDays = s.Rtm.DataDays,
            RtmMinutes = s.Rtm.TreatmentMinutes,
            Attention = top.HasValue ? StatusDisplay.Attention(top.Value) : null,
            AttentionSummary = string.Join("; ", s.Attention.Where(a => a.Severity >= AttentionSeverity.Warning).Select(a => a.Title)),
            NeedsAttention = top.HasValue,
            RtmNeedsFollowUp = p.Status == PatientStatus.Active && (s.Rtm.Readiness is RtmReadiness.AtRisk or RtmReadiness.BehindPace
                || (s.Rtm.Readiness == RtmReadiness.OnTrack && !s.Rtm.HasInteractiveCommunication))
        };
    }
}
