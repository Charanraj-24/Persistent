using Reflect.Domain.ProviderDashboard.Repositories;

namespace Reflect.Web.Infrastructure;

/// <summary>
/// Groups the Provider Dashboard repositories the way the real IUnitOfWork facade does (unitOfWork.&lt;Entity&gt;Repo).
/// On merge these properties move onto the real facade.
/// </summary>
public class ProviderDashboardRepositories
{
    public ProviderDashboardRepositories(
        IProviderRepository providerRepo, IPatientRepository patientRepo, IActivitySessionRepository activitySessionRepo,
        IAssessmentRepository assessmentRepo, ITreatmentGoalRepository treatmentGoalRepo, ICareCommunicationRepository careCommunicationRepo,
        IMonitoringTimeEntryRepository monitoringTimeEntryRepo, IPhysiologicReadingRepository physiologicReadingRepo)
    {
        ProviderRepo = providerRepo;
        PatientRepo = patientRepo;
        ActivitySessionRepo = activitySessionRepo;
        AssessmentRepo = assessmentRepo;
        TreatmentGoalRepo = treatmentGoalRepo;
        CareCommunicationRepo = careCommunicationRepo;
        MonitoringTimeEntryRepo = monitoringTimeEntryRepo;
        PhysiologicReadingRepo = physiologicReadingRepo;
    }

    public IProviderRepository ProviderRepo { get; }
    public IPatientRepository PatientRepo { get; }
    public IActivitySessionRepository ActivitySessionRepo { get; }
    public IAssessmentRepository AssessmentRepo { get; }
    public ITreatmentGoalRepository TreatmentGoalRepo { get; }
    public ICareCommunicationRepository CareCommunicationRepo { get; }
    public IMonitoringTimeEntryRepository MonitoringTimeEntryRepo { get; }
    public IPhysiologicReadingRepository PhysiologicReadingRepo { get; }
}
