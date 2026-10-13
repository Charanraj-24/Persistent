using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Repositories;

namespace Reflect.Web.Infrastructure.InMemory;

// DEMO ONLY: in-memory repositories. Every query filters soft-deleted rows, like the real EF Core query filters.

public abstract class InMemoryRepositoryBase
{
    protected InMemoryRepositoryBase(InMemoryProviderDashboardStore store) => Store = store;

    protected InMemoryProviderDashboardStore Store { get; }

    protected Task<List<T>> Query<T>(IEnumerable<T> source, Func<T, bool> predicate) where T : BaseEntity
    {
        lock (Store.Sync)
        {
            return Task.FromResult(source.Where(e => !e.IsDeleted && predicate(e)).ToList());
        }
    }

    protected Task<T> Single<T>(IEnumerable<T> source, Func<T, bool> predicate) where T : BaseEntity
    {
        lock (Store.Sync)
        {
            return Task.FromResult(source.FirstOrDefault(e => !e.IsDeleted && predicate(e)));
        }
    }

    protected Task Add<T>(List<T> target, T entity) where T : BaseEntity
    {
        lock (Store.Sync)
        {
            target.Add(entity);
        }
        return Task.CompletedTask;
    }
}

public class InMemoryProviderRepository : InMemoryRepositoryBase, IProviderRepository
{
    public InMemoryProviderRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<Provider> GetByIdAsync(Guid id, CancellationToken ct) => Single(Store.Providers, p => p.Id == id);

    public Task<List<Provider>> GetAllAsync(CancellationToken ct) => Query(Store.Providers, _ => true);
}

public class InMemoryPatientRepository : InMemoryRepositoryBase, IPatientRepository
{
    public InMemoryPatientRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<Patient> GetForProviderAsync(Guid patientId, Guid providerId, CancellationToken ct) =>
        Single(Store.Patients, p => p.Id == patientId && p.ProviderId == providerId);

    public Task<List<Patient>> GetByProviderAsync(Guid providerId, CancellationToken ct) =>
        Query(Store.Patients, p => p.ProviderId == providerId);
}

public class InMemoryActivitySessionRepository : InMemoryRepositoryBase, IActivitySessionRepository
{
    public InMemoryActivitySessionRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<List<ActivitySession>> GetByPatientAsync(Guid patientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct) =>
        Query(Store.Sessions, s => s.PatientId == patientId && s.StartedAt >= fromUtc && s.StartedAt < toUtc);

    public Task<List<ActivitySession>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        return Query(Store.Sessions, s => ids.Contains(s.PatientId) && s.StartedAt >= fromUtc && s.StartedAt < toUtc);
    }
}

public class InMemoryAssessmentRepository : InMemoryRepositoryBase, IAssessmentRepository
{
    public InMemoryAssessmentRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<List<Assessment>> GetByPatientAsync(Guid patientId, CancellationToken ct) =>
        Query(Store.Assessments, a => a.PatientId == patientId);

    public Task<List<Assessment>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        return Query(Store.Assessments, a => ids.Contains(a.PatientId));
    }
}

public class InMemoryTreatmentGoalRepository : InMemoryRepositoryBase, ITreatmentGoalRepository
{
    public InMemoryTreatmentGoalRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<TreatmentGoal> GetByIdAsync(Guid id, CancellationToken ct) => Single(Store.Goals, g => g.Id == id);

    public Task<List<TreatmentGoal>> GetByPatientAsync(Guid patientId, CancellationToken ct) =>
        Query(Store.Goals, g => g.PatientId == patientId);

    public Task<List<TreatmentGoal>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        return Query(Store.Goals, g => ids.Contains(g.PatientId));
    }

    public Task AddAsync(TreatmentGoal goal, CancellationToken ct) => Add(Store.Goals, goal);

    public Task UpdateAsync(TreatmentGoal goal, CancellationToken ct)
    {
        lock (Store.Sync)
        {
            var index = Store.Goals.FindIndex(g => g.Id == goal.Id);
            if (index < 0) throw new EntityNotFoundException("Goal not found.");
            Store.Goals[index] = goal;
        }
        return Task.CompletedTask;
    }
}

public class InMemoryCareCommunicationRepository : InMemoryRepositoryBase, ICareCommunicationRepository
{
    public InMemoryCareCommunicationRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<List<CareCommunication>> GetByPatientAsync(Guid patientId, CancellationToken ct) =>
        Query(Store.Communications, c => c.PatientId == patientId);

    public Task<List<CareCommunication>> GetByPatientsAsync(IReadOnlyCollection<Guid> patientIds, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var ids = patientIds.ToHashSet();
        return Query(Store.Communications, c => ids.Contains(c.PatientId) && c.OccurredAt >= fromUtc && c.OccurredAt < toUtc);
    }

    public Task AddAsync(CareCommunication communication, CancellationToken ct) => Add(Store.Communications, communication);
}

public class InMemoryMonitoringTimeEntryRepository : InMemoryRepositoryBase, IMonitoringTimeEntryRepository
{
    public InMemoryMonitoringTimeEntryRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<MonitoringTimeEntry> GetByIdAsync(Guid id, CancellationToken ct) => Single(Store.MonitoringEntries, e => e.Id == id);

    public Task<List<MonitoringTimeEntry>> GetByProviderAsync(Guid providerId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct) =>
        Query(Store.MonitoringEntries, e => e.ProviderId == providerId && e.StartedAt >= fromUtc && e.StartedAt < toUtc);

    public Task<List<MonitoringTimeEntry>> GetByPatientAsync(Guid patientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct) =>
        Query(Store.MonitoringEntries, e => e.PatientId == patientId && e.StartedAt >= fromUtc && e.StartedAt < toUtc);

    public Task AddAsync(MonitoringTimeEntry entry, CancellationToken ct) => Add(Store.MonitoringEntries, entry);

    // Entries are held by reference in memory, so the tracked instance is already current; EF Core would SaveChanges here.
    public Task UpdateAsync(MonitoringTimeEntry entry, CancellationToken ct) => Task.CompletedTask;
}

public class InMemoryPhysiologicReadingRepository : InMemoryRepositoryBase, IPhysiologicReadingRepository
{
    public InMemoryPhysiologicReadingRepository(InMemoryProviderDashboardStore store) : base(store) { }

    public Task<List<PhysiologicReading>> GetByPatientAsync(Guid patientId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct) =>
        Query(Store.PhysiologicReadings, r => r.PatientId == patientId && r.RecordedAt >= fromUtc && r.RecordedAt < toUtc);
}

public static class InMemoryRegistration
{
    public static IServiceCollection AddProviderDashboardInMemoryData(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryProviderDashboardStore>(sp =>
        {
            var store = new InMemoryProviderDashboardStore();
            SyntheticDataSeeder.Seed(store, sp.GetRequiredService<TimeProvider>().GetUtcNow());
            return store;
        });
        services.AddScoped<IProviderRepository, InMemoryProviderRepository>();
        services.AddScoped<IPatientRepository, InMemoryPatientRepository>();
        services.AddScoped<IActivitySessionRepository, InMemoryActivitySessionRepository>();
        services.AddScoped<IAssessmentRepository, InMemoryAssessmentRepository>();
        services.AddScoped<ITreatmentGoalRepository, InMemoryTreatmentGoalRepository>();
        services.AddScoped<ICareCommunicationRepository, InMemoryCareCommunicationRepository>();
        services.AddScoped<IMonitoringTimeEntryRepository, InMemoryMonitoringTimeEntryRepository>();
        services.AddScoped<IPhysiologicReadingRepository, InMemoryPhysiologicReadingRepository>();
        services.AddScoped<Reflect.Web.Infrastructure.ProviderDashboardRepositories>();
        return services;
    }
}
