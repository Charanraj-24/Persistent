using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class ClientIndexViewModel
{
    public string MonthLabel { get; set; } = string.Empty;
    public List<ClientRowViewModel> Rows { get; set; } = new();

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
        MonthLabel = snap.Today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        Rows = snap.Clients.Where(c => c.Status != ClientRtmStatus.Discharged)
            .Select(c => ClientRowViewModel.FromEntity(c, snap.EvaluateRtm(c), snap.Summarize(c), snap.FlagsFor(c)))
            .ToList();
    }
}
