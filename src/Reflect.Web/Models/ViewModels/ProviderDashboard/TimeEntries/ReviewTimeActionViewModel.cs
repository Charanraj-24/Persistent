using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.TimeEntries;

/// <summary>Attest all pending auto-tracked time for one client this month, or discard one pending entry.</summary>
public class ReviewTimeActionViewModel
{
    public Guid ClientId { get; set; }
    public Guid? EntryId { get; set; }

    public async Task<ReviewTimeActionSaveData> PrepareDataForAttest(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IRtmTimeEntryRepository entries, CancellationToken ct)
    {
        var (provider, client) = await LoadAsync(currentUser, providers, clients, ct);
        var nowUtc = DateTimeOffset.UtcNow;
        var month = RtmPeriods.CalendarMonthOf(EasternTime.Today(nowUtc));
        var pending = (await entries.GetForClientAsync(client.Id, EasternTime.StartOfDayUtc(month.Start), nowUtc, ct))
            .Where(e => !e.IsAttested && e.ProviderId == provider.Id && e.ActiveSeconds > 0).ToList();
        foreach (var e in pending)
        {
            e.IsAttested = true;
            e.AttestedAt = nowUtc;
        }
        return new ReviewTimeActionSaveData { Entries = pending, Seconds = pending.Sum(e => e.ActiveSeconds) };
    }

    public async Task<ReviewTimeActionSaveData> PrepareDataForDiscard(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IRtmTimeEntryRepository entries, CancellationToken ct)
    {
        var (provider, client) = await LoadAsync(currentUser, providers, clients, ct);
        if (!EntryId.HasValue) return null;
        var entry = await entries.GetByIdAsync(EntryId.Value, ct);
        if (entry.ProviderId != provider.Id || entry.ClientId != client.Id) throw new EntityNotFoundException("Time entry not found.");
        if (entry.IsAttested) return null;
        entry.IsDeleted = true;
        entry.DateDeleted = DateTimeOffset.UtcNow;
        return new ReviewTimeActionSaveData { Entries = new List<RtmTimeEntry> { entry }, Seconds = entry.ActiveSeconds };
    }

    private async Task<(ProviderProfile, ProviderClient)> LoadAsync(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var client = await clients.GetByIdAsync(ClientId, ct);
        if (client.ProviderId != provider.Id) throw new EntityNotFoundException("Client not found.");
        return (provider, client);
    }
}
