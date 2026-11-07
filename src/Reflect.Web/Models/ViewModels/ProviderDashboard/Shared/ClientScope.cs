using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

/// <summary>
/// The signed-in provider plus one of their clients, with "now" pinned for the request.
/// Throws <see cref="EntityNotFoundException"/> when the client belongs to another provider, so it looks like a 404.
/// </summary>
public sealed class ClientScope
{
    public ProviderProfile Provider { get; private init; }
    public ProviderClient Client { get; private init; }
    public DateTimeOffset NowUtc { get; private init; }
    public DateOnly Today { get; private init; }
    public Guid[] UserIds => new[] { Client.ClientUserId };

    public DateTimeOffset DaysAgoUtc(int days) => EasternTime.StartOfDayUtc(Today.AddDays(-days));

    public static async Task<ClientScope> LoadAsync(Guid clientId, ICurrentUserService currentUser,
        IProviderProfileRepository providers, IProviderClientRepository clients, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var client = await clients.GetByIdAsync(clientId, ct);
        if (client.ProviderId != provider.Id) throw new EntityNotFoundException("Client not found.");
        var now = DateTimeOffset.UtcNow;
        return new ClientScope { Provider = provider, Client = client, NowUtc = now, Today = EasternTime.Today(now) };
    }
}
