using Reflect.Domain.ProviderDashboard.Entities;

namespace Reflect.Domain.ProviderDashboard.Repositories;

// One interface per entity, async with a CancellationToken. Every query excludes soft-deleted rows.
// The demo backs these with in-memory implementations; the Reflect XR team swaps in EF Core on merge.

public interface IProviderRepository
{
    Task<Provider> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<Provider>> GetAllAsync(CancellationToken ct);
}

public interface IPatientRepository
{
    /// <summary>Returns the patient only when it belongs to the provider; otherwise null.</summary>
    Task<Patient> GetForProviderAsync(Guid patientId, Guid providerId, CancellationToken ct);
    Task<List<Patient>> GetByProviderAsync(Guid providerId, CancellationToken ct);
}

public interface IActivitySessionRepository
{
    Task<List<ActivitySession>> GetByPatientAsync(Guid patientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task<List<ActivitySession>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}

public interface IAssessmentRepository
{
    Task<List<Assessment>> GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<Assessment>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct);
}

public interface ITreatmentGoalRepository
{
    Task<TreatmentGoal> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<TreatmentGoal>> GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<TreatmentGoal>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct);
    Task AddAsync(TreatmentGoal goal, CancellationToken ct);
    Task UpdateAsync(TreatmentGoal goal, CancellationToken ct);
}

public interface ICareCommunicationRepository
{
    Task<List<CareCommunication>> GetByPatientAsync(Guid patientId, CancellationToken ct);
    Task<List<CareCommunication>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task AddAsync(CareCommunication communication, CancellationToken ct);
}

public interface IMonitoringTimeEntryRepository
{
    Task<MonitoringTimeEntry> GetByIdAsync(Guid id, CancellationToken ct);
    Task<List<MonitoringTimeEntry>> GetByProviderAsync(Guid providerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task<List<MonitoringTimeEntry>> GetByPatientAsync(Guid patientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task AddAsync(MonitoringTimeEntry entry, CancellationToken ct);
    Task UpdateAsync(MonitoringTimeEntry entry, CancellationToken ct);
}

public interface IPhysiologicReadingRepository
{
    Task<List<PhysiologicReading>> GetByPatientAsync(Guid patientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
}
