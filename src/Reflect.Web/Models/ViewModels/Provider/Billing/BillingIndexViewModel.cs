using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Billing;

public class BillingIndexViewModel
{
    public BillingPeriod Period { get; set; }
    public bool IsCurrentPeriod { get; set; }
    public int DaysLeft { get; set; }
    public IReadOnlyList<PeriodOption> PeriodOptions { get; set; } = Array.Empty<PeriodOption>();
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderNpi { get; set; } = string.Empty;

    public int Enrolled { get; set; }
    public int Ready { get; set; }
    public int PartialOrOnTrack { get; set; }
    public int AtRiskOrNotBillable { get; set; }
    public int TotalTreatmentMinutes { get; set; }
    public List<CodeTotalViewModel> CodeTotals { get; set; } = new();
    public List<BillingRowViewModel> Rows { get; set; } = new();

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, string periodKey, CancellationToken ct)
    {
        Period = Shared.PeriodOptions.Resolve(periodKey, context.Today);
        IsCurrentPeriod = Period.Contains(context.Today);
        DaysLeft = IsCurrentPeriod ? Period.End.DayNumber - context.Today.DayNumber + 1 : 0;
        PeriodOptions = Shared.PeriodOptions.List(Period, context.Today);

        var caseload = await CaseloadSnapshot.LoadAsync(repos, context, Period, ct);
        ProviderName = caseload.Provider.DisplayName;
        ProviderNpi = caseload.Provider.Npi;

        Rows = caseload.Patients
            .Where(p => p.Patient.Status != PatientStatus.Discharged)
            .Select(BillingRowViewModel.FromSnapshot)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.PatientName)
            .ToList();

        var enrolled = caseload.Patients.Where(p => p.Rtm.Readiness != RtmReadiness.NotEnrolled).ToList();
        Enrolled = enrolled.Count;
        Ready = enrolled.Count(p => p.Rtm.Readiness == RtmReadiness.Ready);
        PartialOrOnTrack = enrolled.Count(p => p.Rtm.Readiness is RtmReadiness.Partial or RtmReadiness.OnTrack);
        AtRiskOrNotBillable = enrolled.Count(p => p.Rtm.Readiness is RtmReadiness.AtRisk or RtmReadiness.NotBillable or RtmReadiness.BehindPace);
        TotalTreatmentMinutes = enrolled.Sum(p => p.Rtm.TreatmentMinutes);

        CodeTotals = enrolled
            .SelectMany(p => p.Rtm.Codes)
            .GroupBy(c => c.Code)
            .Select(g => new CodeTotalViewModel { Code = g.Key, Description = RtmBillingRules.Describe(g.Key), Units = g.Sum(c => c.Units), Patients = g.Count() })
            .OrderBy(c => c.Code)
            .ToList();
    }

    public string ToCsv()
    {
        var csv = new CsvBuilder()
            .Row("Billing period", Period.Label)
            .Row("Provider", ProviderName, "NPI", ProviderNpi)
            .Row("Note", "Decision support only. Synthetic data. Confirm codes with your billing team before submitting.")
            .Row()
            .Row("Patient", "Chart number", "Status", "Remote data days", "Monitoring minutes", "Interactive minutes", "Treatment minutes", "Interactive contact", "Codes", "Open items");
        foreach (var r in Rows)
        {
            csv.Row(r.PatientName, r.ChartNumber, r.Status.Text, r.DataDays, r.MonitoringMinutes, r.InteractiveMinutes, r.TreatmentMinutes,
                r.HasInteractive ? "Yes" : "No", string.Join(" ", r.Codes.Select(c => c.Units > 1 ? $"{c.Code}x{c.Units}" : c.Code)), string.Join(" | ", r.Blockers));
        }
        return csv.ToString();
    }
}

public class CodeTotalViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Units { get; set; }
    public int Patients { get; set; }
}

public class BillingRowViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string ChartNumber { get; set; } = string.Empty;
    public StatusBadge Status { get; set; }
    public int SortOrder { get; set; }
    public int DataDays { get; set; }
    public int DataDaysRequired { get; set; }
    public int MonitoringMinutes { get; set; }
    public int InteractiveMinutes { get; set; }
    public int TreatmentMinutes { get; set; }
    public bool HasInteractive { get; set; }
    public bool Enrolled { get; set; }
    public int? NextThreshold { get; set; }
    public int MinutesToNext { get; set; }
    public List<BillableCode> Codes { get; set; } = new();
    public List<string> Blockers { get; set; } = new();

    public static BillingRowViewModel FromSnapshot(PatientSnapshot s) => new()
    {
        PatientId = s.Patient.Id,
        PatientName = s.Patient.FullName,
        Initials = s.Patient.Initials,
        ChartNumber = s.Patient.ChartNumber,
        Status = StatusDisplay.Rtm(s.Rtm.Readiness),
        SortOrder = s.Rtm.Readiness switch
        {
            RtmReadiness.AtRisk => 0,
            RtmReadiness.NotBillable => 1,
            RtmReadiness.BehindPace => 2,
            RtmReadiness.OnTrack => 3,
            RtmReadiness.Partial => 4,
            RtmReadiness.Ready => 5,
            _ => 6
        },
        DataDays = s.Rtm.DataDays,
        DataDaysRequired = s.Rtm.DataDaysRequired,
        MonitoringMinutes = s.Rtm.MonitoringMinutes,
        InteractiveMinutes = s.Rtm.InteractiveMinutes,
        TreatmentMinutes = s.Rtm.TreatmentMinutes,
        HasInteractive = s.Rtm.HasInteractiveCommunication,
        Enrolled = s.Rtm.Readiness != RtmReadiness.NotEnrolled,
        NextThreshold = s.Rtm.NextThresholdMinutes,
        MinutesToNext = s.Rtm.MinutesToNextThreshold,
        Codes = s.Rtm.Codes.ToList(),
        Blockers = s.Rtm.Blockers.ToList()
    };
}
