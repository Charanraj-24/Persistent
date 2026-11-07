using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

/// <summary>Dashboard tab: measurement-based-care outcomes across the caseload (PHQ-9 and GAD-7).</summary>
public class DashboardOutcomesTabViewModel
{
    public OutcomeSummary Phq { get; set; }
    public OutcomeSummary Gad { get; set; }
    public List<OutcomeRowViewModel> Rows { get; set; } = new();
    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries,
        CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        var list = snap.Clients.Where(c => c.Status != ClientRtmStatus.Discharged).ToList();
        Phq = OutcomesCalculator.Summarize(list.Select(c => snap.AssessmentsFor(c)), AssessmentInstrument.Phq9);
        Gad = OutcomesCalculator.Summarize(list.Select(c => snap.AssessmentsFor(c)), AssessmentInstrument.Gad7);

        var points = new List<object>();
        foreach (var c in list)
        {
            var phq = OutcomesCalculator.ForClient(snap.AssessmentsFor(c), AssessmentInstrument.Phq9);
            var gad = OutcomesCalculator.ForClient(snap.AssessmentsFor(c), AssessmentInstrument.Gad7);
            if (phq == null && gad == null) continue;
            var activeDays = snap.Summarize(c).ActiveDays;
            Rows.Add(OutcomeRowViewModel.From(c, phq, gad, activeDays));
            if (phq != null) points.Add(new { x = activeDays, y = phq.Change, name = c.FullName });
        }
        Rows = Rows.OrderBy(r => r.PhqChangeSort).ToList();

        ChartDataJson = JsonSerializer.Serialize(new
        {
            beforeAfter = new
            {
                labels = new[] { "PHQ-9", "GAD-7" },
                baseline = new[] { Math.Round(Phq.AverageBaseline, 1), Math.Round(Gad.AverageBaseline, 1) },
                latest = new[] { Math.Round(Phq.AverageLatest, 1), Math.Round(Gad.AverageLatest, 1) }
            },
            scatter = points
        });
    }
}
