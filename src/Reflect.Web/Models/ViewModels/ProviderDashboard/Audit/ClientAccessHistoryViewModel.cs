using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Audit;

/// <summary>Treating provider: who opened this client's record recently (supports an accounting-of-access request).</summary>
public class ClientAccessHistoryViewModel
{
    public List<AuditRowViewModel> Rows { get; set; } = new();

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAccessAuditRepository audit, CancellationToken ct)
    {
        await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct); // ownership check
        Rows = (await audit.GetForClientAsync(clientId, 25, ct)).Select(AuditRowViewModel.From).ToList();
    }
}
