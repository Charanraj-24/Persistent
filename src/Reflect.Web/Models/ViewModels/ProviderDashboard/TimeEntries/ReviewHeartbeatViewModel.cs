using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.TimeEntries;

/// <summary>AJAX heartbeat from the active-time tracker on the client page.</summary>
public class ReviewHeartbeatViewModel
{
    public Guid ClientId { get; set; }
    public string TrackingKey { get; set; } = string.Empty;
    public int ActiveSeconds { get; set; }

    public async Task<ReviewHeartbeatSaveData> PrepareDataForSave(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IRtmTimeEntryRepository entries, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(TrackingKey) || TrackingKey.Length > 64) return null;

        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var client = await clients.GetByIdAsync(ClientId, ct);
        if (client.ProviderId != provider.Id) throw new EntityNotFoundException("Client not found.");

        var nowUtc = DateTimeOffset.UtcNow;
        var existing = await entries.GetByTrackingKeyAsync(provider.Id, TrackingKey, ct);
        if (existing != null && (existing.ClientId != client.Id || existing.IsAttested)) return null;

        var increment = ReviewTimeRules.AcceptedIncrement(ActiveSeconds, existing?.LastHeartbeatAt, nowUtc);
        if (existing == null)
        {
            return new ReviewHeartbeatSaveData
            {
                IsNew = true,
                AcceptedSeconds = increment,
                Entry = new RtmTimeEntry
                {
                    ProviderId = provider.Id,
                    ClientId = client.Id,
                    StartedAt = nowUtc.AddSeconds(-increment),
                    ActiveSeconds = increment,
                    Activity = RtmActivityType.DataReview,
                    Source = RtmTimeSource.AutoTracked,
                    Note = "Dashboard review.",
                    TrackingKey = TrackingKey,
                    LastHeartbeatAt = nowUtc
                }
            };
        }

        existing.ActiveSeconds += increment;
        existing.LastHeartbeatAt = nowUtc;
        return new ReviewHeartbeatSaveData { IsNew = false, AcceptedSeconds = increment, Entry = existing };
    }
}
