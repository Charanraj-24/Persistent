// DEMO ONLY: in-memory repository implementations. On merge these are replaced by EF Core repositories
// (clinical data) and a BigQuery-backed reader (AppSession analytics). Interfaces stay the same.
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces.Repositories;

namespace Reflect.Web.Data.InMemory;

/// <summary>Singleton holding synthetic data. Writes are serialized with a lock.</summary>
public class InMemoryProviderDashboardStore
{
    public readonly object Sync = new();
    public List<ProviderProfile> Providers { get; } = new();
    public List<ProviderClient> Clients { get; } = new();
    public List<AppSession> Sessions { get; } = new();
    public List<MoodCheckIn> CheckIns { get; } = new();
    public List<AssessmentResult> Assessments { get; } = new();
    public List<Observation> Observations { get; } = new();
    public List<RtmTimeEntry> TimeEntries { get; } = new();
    public List<IngestEvent> Events { get; } = new();
    public List<AccessAuditEntry> Audit { get; } = new();
}

public class InMemoryProviderProfileRepository : IProviderProfileRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryProviderProfileRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<ProviderProfile> GetByUserIdAsync(Guid userId, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            var p = _store.Providers.FirstOrDefault(x => !x.IsDeleted && x.UserId == userId)
                ?? throw new EntityNotFoundException("Provider profile not found.");
            return Task.FromResult(p);
        }
    }

    public Task<List<ProviderProfile>> GetByFacilityAsync(Guid facilityId, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.Providers.Where(x => !x.IsDeleted && x.FacilityId == facilityId).OrderBy(x => x.FullName).ToList());
    }
}

public class InMemoryProviderClientRepository : IProviderClientRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryProviderClientRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<List<ProviderClient>> GetByProviderAsync(Guid providerId, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.Clients.Where(c => !c.IsDeleted && c.ProviderId == providerId).ToList());
    }

    public Task<ProviderClient> GetByIdAsync(Guid id, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            var c = _store.Clients.FirstOrDefault(x => !x.IsDeleted && x.Id == id)
                ?? throw new EntityNotFoundException("Client not found.");
            return Task.FromResult(c);
        }
    }

    public Task<ProviderClient> GetByClientUserIdAsync(Guid clientUserId, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            var c = _store.Clients.FirstOrDefault(x => !x.IsDeleted && x.ClientUserId == clientUserId)
                ?? throw new EntityNotFoundException("Client not found.");
            return Task.FromResult(c);
        }
    }
}

public class InMemoryAppSessionRepository : IAppSessionRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryAppSessionRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<List<AppSession>> GetForClientsAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var ids = clientUserIds.ToHashSet();
        lock (_store.Sync)
            return Task.FromResult(_store.Sessions.Where(s => !s.IsDeleted && ids.Contains(s.ClientUserId) && s.StartedAt >= fromUtc && s.StartedAt <= toUtc)
                .OrderBy(s => s.StartedAt).ToList());
    }

    public Task AddAsync(AppSession session, CancellationToken ct)
    {
        lock (_store.Sync) _store.Sessions.Add(session);
        return Task.CompletedTask;
    }
}

public class InMemoryMoodCheckInRepository : IMoodCheckInRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryMoodCheckInRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<List<MoodCheckIn>> GetForClientsAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var ids = clientUserIds.ToHashSet();
        lock (_store.Sync)
            return Task.FromResult(_store.CheckIns.Where(c => !c.IsDeleted && ids.Contains(c.ClientUserId) && c.RecordedAt >= fromUtc && c.RecordedAt <= toUtc)
                .OrderBy(c => c.RecordedAt).ToList());
    }

    public Task AddAsync(MoodCheckIn checkIn, CancellationToken ct)
    {
        lock (_store.Sync) _store.CheckIns.Add(checkIn);
        return Task.CompletedTask;
    }
}

public class InMemoryAssessmentResultRepository : IAssessmentResultRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryAssessmentResultRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<List<AssessmentResult>> GetForClientsAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var ids = clientUserIds.ToHashSet();
        lock (_store.Sync)
            return Task.FromResult(_store.Assessments.Where(a => !a.IsDeleted && ids.Contains(a.ClientUserId) && a.CompletedAt >= fromUtc && a.CompletedAt <= toUtc)
                .OrderBy(a => a.CompletedAt).ToList());
    }

    public Task AddAsync(AssessmentResult result, CancellationToken ct)
    {
        lock (_store.Sync) _store.Assessments.Add(result);
        return Task.CompletedTask;
    }
}

public class InMemoryObservationRepository : IObservationRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryObservationRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<List<Observation>> GetForClientAsync(Guid clientUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.Observations.Where(o => !o.IsDeleted && o.ClientUserId == clientUserId && o.EffectiveAt >= fromUtc && o.EffectiveAt <= toUtc)
                .OrderBy(o => o.EffectiveAt).ToList());
    }

    public Task AddAsync(Observation observation, CancellationToken ct)
    {
        lock (_store.Sync) _store.Observations.Add(observation);
        return Task.CompletedTask;
    }
}

public class InMemoryRtmTimeEntryRepository : IRtmTimeEntryRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryRtmTimeEntryRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task<List<RtmTimeEntry>> GetForProviderAsync(Guid providerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.TimeEntries.Where(e => !e.IsDeleted && e.ProviderId == providerId && e.StartedAt >= fromUtc && e.StartedAt <= toUtc)
                .OrderBy(e => e.StartedAt).ToList());
    }

    public Task<List<RtmTimeEntry>> GetForClientAsync(Guid clientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.TimeEntries.Where(e => !e.IsDeleted && e.ClientId == clientId && e.StartedAt >= fromUtc && e.StartedAt <= toUtc)
                .OrderBy(e => e.StartedAt).ToList());
    }

    public Task<RtmTimeEntry> GetByIdAsync(Guid id, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            var e = _store.TimeEntries.FirstOrDefault(x => !x.IsDeleted && x.Id == id)
                ?? throw new EntityNotFoundException("Time entry not found.");
            return Task.FromResult(e);
        }
    }

    public Task<RtmTimeEntry> GetByTrackingKeyAsync(Guid providerId, string trackingKey, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.TimeEntries.FirstOrDefault(x => !x.IsDeleted && x.ProviderId == providerId && x.TrackingKey == trackingKey));
    }

    public Task AddAsync(RtmTimeEntry entry, CancellationToken ct)
    {
        lock (_store.Sync) _store.TimeEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RtmTimeEntry entry, CancellationToken ct)
    {
        // Entities are held by reference in memory; nothing to copy. EF Core will track changes on merge.
        return Task.CompletedTask;
    }
}

public class InMemoryIngestEventRepository : IIngestEventRepository
{
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryIngestEventRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task AddAsync(IngestEvent ingestEvent, CancellationToken ct)
    {
        lock (_store.Sync) _store.Events.Add(ingestEvent);
        return Task.CompletedTask;
    }

    public Task<List<IngestEvent>> GetAfterAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset afterUtc, int take, CancellationToken ct)
    {
        var ids = clientUserIds.ToHashSet();
        lock (_store.Sync)
            return Task.FromResult(_store.Events.Where(e => !e.IsDeleted && ids.Contains(e.ClientUserId) && e.ReceivedAt > afterUtc)
                .OrderBy(e => e.ReceivedAt).Take(take).ToList());
    }

    public Task<List<IngestEvent>> GetRecentAsync(IReadOnlyCollection<Guid> clientUserIds, int take, CancellationToken ct)
    {
        var ids = clientUserIds.ToHashSet();
        lock (_store.Sync)
            return Task.FromResult(_store.Events.Where(e => !e.IsDeleted && ids.Contains(e.ClientUserId))
                .OrderByDescending(e => e.ReceivedAt).Take(take).ToList());
    }
}

public class InMemoryAccessAuditRepository : IAccessAuditRepository
{
    private const int MaxEntries = 20000; // demo memory cap; production storage is append-only and retained per policy (6 years)
    private readonly InMemoryProviderDashboardStore _store;
    public InMemoryAccessAuditRepository(InMemoryProviderDashboardStore store) => _store = store;

    public Task AddAsync(AccessAuditEntry entry, CancellationToken ct)
    {
        lock (_store.Sync)
        {
            _store.Audit.Add(entry);
            if (_store.Audit.Count > MaxEntries) _store.Audit.RemoveRange(0, _store.Audit.Count - MaxEntries);
        }
        return Task.CompletedTask;
    }

    public Task<List<AccessAuditEntry>> GetRecentAsync(DateTimeOffset fromUtc, int take, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.Audit.Where(e => e.OccurredAt >= fromUtc).OrderByDescending(e => e.OccurredAt).Take(take).ToList());
    }

    public Task<List<AccessAuditEntry>> GetForClientAsync(Guid clientId, int take, CancellationToken ct)
    {
        lock (_store.Sync)
            return Task.FromResult(_store.Audit.Where(e => e.ClientId == clientId).OrderByDescending(e => e.OccurredAt).Take(take).ToList());
    }
}
