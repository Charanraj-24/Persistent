using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard;

/// <summary>
/// Dashboard shell: page header and KPI tiles. Each section below the KPIs is a tab loaded on first open
/// by <c>HomeController.Tab</c>.
/// </summary>
public class ProviderDashboardViewModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public int DaysLeftInMonth { get; set; }

    public int ActiveClients { get; set; }
    public int BillableClients { get; set; }
    public int AttestedMinutes { get; set; }
    public int PendingMinutes { get; set; }
    public int ClientsNeedingAttention { get; set; }
    public int UrgentFlags { get; set; }
    public int OpenTodos { get; set; }
    public ReimbursementEstimate Estimate { get; set; } = new(0, 0);

    public static readonly IReadOnlyList<(string Key, string Label, string Icon)> Tabs = new[]
    {
        ("attention", "Needs attention", "ri-stethoscope-line"),
        ("todos", "RTM to-dos", "ri-list-check"),
        ("trends", "Caseload trends", "ri-line-chart-line"),
        ("outcomes", "Outcomes", "ri-award-line"),
        ("usage", "Module usage", "ri-apps-2-line"),
        ("activity", "Recent activity", "ri-time-line")
    };

    public async Task PrepareData(
        ICurrentUserService currentUser,
        IProviderProfileRepository providers,
        IProviderClientRepository clients,
        IAppSessionRepository sessions,
        IMoodCheckInRepository checkIns,
        IAssessmentResultRepository assessments,
        IRtmTimeEntryRepository timeEntries,
        CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        var month = RtmPeriods.CalendarMonthOf(snap.Today);

        ProviderName = snap.Provider.FullName;
        FacilityName = snap.Provider.FacilityName;
        MonthLabel = snap.Today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        DaysLeftInMonth = month.End.DayNumber - snap.Today.DayNumber;

        var codes = new List<BillableCode>();
        foreach (var c in snap.Clients.Where(c => c.Status == ClientRtmStatus.Active))
        {
            ActiveClients++;
            var eval = snap.EvaluateRtm(c);
            codes.AddRange(eval.Codes);
            if (eval.Status == RtmBillingStatus.Billable) BillableClients++;
            AttestedMinutes += eval.ManagementMinutes;
            PendingMinutes += eval.PendingMinutes;
            if (!string.IsNullOrEmpty(RtmStatusPresenter.NextStep(eval))) OpenTodos++;

            var flags = snap.FlagsFor(c).Where(f => f.Severity is FlagSeverity.Critical or FlagSeverity.Warning).ToList();
            if (flags.Count > 0) ClientsNeedingAttention++;
            UrgentFlags += flags.Count(f => f.Severity == FlagSeverity.Critical);
        }
        Estimate = RtmFeeSchedule.Estimate(codes);
    }
}
