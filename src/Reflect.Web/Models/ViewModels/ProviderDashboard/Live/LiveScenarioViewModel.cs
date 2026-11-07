using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Simulation;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Live;

/// <summary>AJAX: run one demo scenario for one of the provider's clients.</summary>
public class LiveScenarioViewModel
{
    public Guid ClientId { get; set; }
    public string Scenario { get; set; } = string.Empty;

    /// <summary>Returns the client to act on, or null when the scenario is unknown. Ownership is enforced.</summary>
    public async Task<ProviderClient> PrepareDataForSave(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, CancellationToken ct)
    {
        if (DemoScenarios.All.All(s => s.Key != Scenario)) return null;
        var scope = await ClientScope.LoadAsync(ClientId, currentUser, providers, clients, ct);
        return scope.Client;
    }
}
