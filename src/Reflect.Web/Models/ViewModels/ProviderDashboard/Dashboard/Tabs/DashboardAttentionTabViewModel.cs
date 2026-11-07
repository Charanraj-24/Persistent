using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

/// <summary>Dashboard tab: clients with urgent or review flags (safety first), plus clients showing progress.</summary>
public class DashboardAttentionTabViewModel
{
    public List<AttentionRowViewModel> Attention { get; set; } = new();
    public List<AttentionRowViewModel> Progress { get; set; } = new();

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries,
        CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        foreach (var c in snap.Clients.Where(c => c.Status == ClientRtmStatus.Active))
        {
            var flags = snap.FlagsFor(c);
            var concerns = flags.Where(f => f.Severity is FlagSeverity.Critical or FlagSeverity.Warning).ToList();
            if (concerns.Count > 0)
                Attention.Add(new AttentionRowViewModel { ClientId = c.Id, Name = c.FullName, Initials = c.Initials, Flag = concerns[0], MoreCount = concerns.Count - 1 });
            var positive = flags.Where(f => f.Severity == FlagSeverity.Positive).ToList();
            if (positive.Count > 0 && concerns.Count == 0)
                Progress.Add(new AttentionRowViewModel { ClientId = c.Id, Name = c.FullName, Initials = c.Initials, Flag = positive[0], MoreCount = positive.Count - 1 });
        }
        Attention = Attention.OrderBy(a => a.Flag.Severity).ThenBy(a => a.Name).ToList();
        Progress = Progress.OrderBy(a => a.Name).ToList();
    }
}
