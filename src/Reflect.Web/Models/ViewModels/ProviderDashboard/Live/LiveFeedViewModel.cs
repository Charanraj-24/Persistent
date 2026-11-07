using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Live;

/// <summary>JSON feed polled by open Provider pages: new data for this provider's clients since a point in time.</summary>
public class LiveFeedViewModel
{
    public long Now { get; set; }
    public List<LiveEventRowViewModel> Events { get; set; } = new();

    public async Task PrepareData(long? afterMs, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IIngestEventRepository events, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        Now = now.ToUnixTimeMilliseconds();
        if (!afterMs.HasValue) return; // first poll only syncs the clock; no backlog

        // Never look back more than 10 minutes, whatever the browser sends.
        var after = DateTimeOffset.FromUnixTimeMilliseconds(Math.Max(afterMs.Value, now.AddMinutes(-10).ToUnixTimeMilliseconds()));
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var byUser = (await clients.GetByProviderAsync(provider.Id, ct)).ToDictionary(c => c.ClientUserId);
        Events = (await events.GetAfterAsync(byUser.Keys.ToList(), after, 50, ct))
            .Select(e => LiveEventRowViewModel.FromEntity(e, byUser[e.ClientUserId])).ToList();
        if (Events.Count > 0) Now = Math.Max(Now, Events.Max(e => e.At));
    }
}
