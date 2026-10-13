using Reflect.Domain.ProviderDashboard.Entities;

namespace Reflect.Web.Infrastructure.InMemory;

/// <summary>
/// DEMO ONLY: process-lifetime storage behind the repository interfaces. Seeded with synthetic data at startup.
/// The Reflect XR team replaces the repositories with EF Core implementations on merge.
/// </summary>
public class InMemoryProviderDashboardStore
{
    public object Sync { get; } = new();
    public List<Provider> Providers { get; } = new();
    public List<Patient> Patients { get; } = new();
    public List<ActivitySession> Sessions { get; } = new();
    public List<Assessment> Assessments { get; } = new();
    public List<TreatmentGoal> Goals { get; } = new();
    public List<CareCommunication> Communications { get; } = new();
    public List<MonitoringTimeEntry> MonitoringEntries { get; } = new();
    public List<PhysiologicReading> PhysiologicReadings { get; } = new();
}
