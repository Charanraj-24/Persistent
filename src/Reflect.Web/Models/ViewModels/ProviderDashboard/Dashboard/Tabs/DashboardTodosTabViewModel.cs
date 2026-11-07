using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

/// <summary>Dashboard tab: every open RTM next step, blockers first, then closest to a billing threshold.</summary>
public class DashboardTodosTabViewModel
{
    public List<OpportunityRowViewModel> Todos { get; set; } = new();
    public int DaysLeftInMonth { get; set; }

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries,
        CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        DaysLeftInMonth = Reflect.Domain.ProviderDashboard.Rtm.RtmPeriods.CalendarMonthOf(snap.Today).End.DayNumber - snap.Today.DayNumber;

        foreach (var c in snap.Clients.Where(c => c.Status == ClientRtmStatus.Active))
        {
            var eval = snap.EvaluateRtm(c);
            var step = RtmStatusPresenter.NextStep(eval);
            if (string.IsNullOrEmpty(step)) continue;
            Todos.Add(new OpportunityRowViewModel
            {
                ClientId = c.Id, Name = c.FullName, Initials = c.Initials, Message = step,
                Badge = RtmStatusPresenter.StatusBadge(eval),
                SortKey = eval.Blockers.Count > 0 ? 0 : eval.PendingMinutes > 0 ? 1 : 2 + eval.MinutesToNextManagementCode
            });
        }
        Todos = Todos.OrderBy(o => o.SortKey).ThenBy(o => o.Name).ToList();
    }
}
