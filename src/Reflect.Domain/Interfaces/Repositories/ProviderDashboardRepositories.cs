using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Domain.Interfaces.Repositories;

// One interface per entity. All queries exclude soft-deleted rows.
// Demo: in-memory implementations. Merge: EF Core (clinical) and a BigQuery-backed reader (analytics).

public interface IProviderProfileRepository
{
    Task<ProviderProfile> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task<List<ProviderProfile>> GetByFacilityAsync(Guid facilityId, CancellationToken ct);
}

public interface IProviderClientRepository
{
    Task<ProviderClient> GetByClientUserIdAsync(Guid clientUserId, CancellationToken ct);
    Task<List<ProviderClient>> GetByProviderAsync(Guid providerId, CancellationToken ct);
    Task<ProviderClient> GetByIdAsync(Guid id, CancellationToken ct);
}

/// <summary>Product analytics (read-only from the dashboard's point of view).</summary>
public interface IAppSessionRepository
{
    Task<List<AppSession>> GetForClientsAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task AddAsync(AppSession session, CancellationToken ct);
}

public interface IMoodCheckInRepository
{
    Task<List<MoodCheckIn>> GetForClientsAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task AddAsync(MoodCheckIn checkIn, CancellationToken ct);
}

public interface IAssessmentResultRepository
{
    Task<List<AssessmentResult>> GetForClientsAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task AddAsync(AssessmentResult result, CancellationToken ct);
}

public interface IObservationRepository
{
    Task<List<Observation>> GetForClientAsync(Guid clientUserId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task AddAsync(Observation observation, CancellationToken ct);
}

public interface IRtmTimeEntryRepository
{
    Task<List<RtmTimeEntry>> GetForProviderAsync(Guid providerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task<List<RtmTimeEntry>> GetForClientAsync(Guid clientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task<RtmTimeEntry> GetByIdAsync(Guid id, CancellationToken ct);
    Task<RtmTimeEntry> GetByTrackingKeyAsync(Guid providerId, string trackingKey, CancellationToken ct);
    Task AddAsync(RtmTimeEntry entry, CancellationToken ct);
    Task UpdateAsync(RtmTimeEntry entry, CancellationToken ct);
}

/// <summary>Ingestion events (live feed + audit of incoming data).</summary>
public interface IIngestEventRepository
{
    Task AddAsync(IngestEvent ingestEvent, CancellationToken ct);
    /// <summary>Events for these clients received strictly after <paramref name="afterUtc"/>, oldest first.</summary>
    Task<List<IngestEvent>> GetAfterAsync(IReadOnlyCollection<Guid> clientUserIds, DateTimeOffset afterUtc, int take, CancellationToken ct);
    Task<List<IngestEvent>> GetRecentAsync(IReadOnlyCollection<Guid> clientUserIds, int take, CancellationToken ct);
}

public interface IAccessAuditRepository
{
    Task AddAsync(AccessAuditEntry entry, CancellationToken ct);
    Task<List<AccessAuditEntry>> GetRecentAsync(DateTimeOffset fromUtc, int take, CancellationToken ct);
    Task<List<AccessAuditEntry>> GetForClientAsync(Guid clientId, int take, CancellationToken ct);
}
