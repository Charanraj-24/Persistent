using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Simulation;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Live;

/// <summary>Live demo page: trigger app events for a client, run the auto-simulator, see the ingestion log.</summary>
public class LiveDemoViewModel
{
    public List<SelectListItem> ClientOptions { get; set; } = new();
    public IReadOnlyList<DemoScenario> Scenarios { get; set; } = DemoScenarios.All;
    public bool SimulatorEnabled { get; set; }
    public int IntervalSeconds { get; set; }
    public int EventsSent { get; set; }
    public bool ApiConfigured { get; set; }
    public string SampleClientUserId { get; set; } = string.Empty;
    public string SampleClientName { get; set; } = string.Empty;
    public List<LiveEventRowViewModel> RecentEvents { get; set; } = new();

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IIngestEventRepository events, LiveDemoState state, bool apiConfigured, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var list = (await clients.GetByProviderAsync(provider.Id, ct)).Where(c => c.Status != ClientRtmStatus.Discharged)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList();
        ClientOptions = list.Select(c => new SelectListItem($"{c.LastName}, {c.FirstName}", c.Id.ToString())).ToList();

        SimulatorEnabled = state.Enabled && state.ProviderId == provider.Id;
        IntervalSeconds = state.IntervalSeconds;
        EventsSent = state.EventsSent;
        ApiConfigured = apiConfigured;
        var sample = list.FirstOrDefault();
        SampleClientUserId = sample?.ClientUserId.ToString() ?? string.Empty;
        SampleClientName = sample?.FullName ?? string.Empty;

        var byUser = list.ToDictionary(c => c.ClientUserId);
        RecentEvents = (await events.GetRecentAsync(byUser.Keys.ToList(), 30, ct))
            .Select(e => LiveEventRowViewModel.FromEntity(e, byUser[e.ClientUserId])).ToList();
    }
}
