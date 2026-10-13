using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>Everything the caseload screens need about one patient, computed once from the repositories.</summary>
public sealed class PatientSnapshot
{
    public Patient Patient { get; init; }
    public IReadOnlyList<ActivitySession> Sessions { get; init; }
    public IReadOnlyList<Assessment> Assessments { get; init; }
    public EngagementSummary Last30Days { get; init; }
    public int SessionsLast7Days { get; init; }
    public int DaysSinceLastSession { get; init; }
    public Assessment LatestPhq9 { get; init; }
    public Assessment PreviousPhq9 { get; init; }
    public Assessment LatestGad7 { get; init; }
    public RtmBillingSummary Rtm { get; init; }
    public IReadOnlyList<AttentionItem> Attention { get; init; }

    public AttentionSeverity? TopSeverity =>
        Attention.Where(a => a.Severity >= AttentionSeverity.Warning).Select(a => (AttentionSeverity?)a.Severity).Max();

    public int? Phq9Change => LatestPhq9 != null && PreviousPhq9 != null ? LatestPhq9.Score - PreviousPhq9.Score : null;
}

/// <summary>
/// Loads a provider's caseload for one billing period. Shared by the Dashboard, Patients, and Billing ViewModels.
/// Read-only: it never saves.
/// </summary>
public sealed class CaseloadSnapshot
{
    /// <summary>How far back engagement history is loaded.</summary>
    public const int HistoryDays = 126;

    public BillingPeriod Period { get; private init; }
    public DateOnly Today { get; private init; }
    public Domain.ProviderDashboard.Entities.Provider Provider { get; private init; }
    public IReadOnlyList<PatientSnapshot> Patients { get; private init; } = Array.Empty<PatientSnapshot>();
    public IReadOnlyList<ActivitySession> AllSessions { get; private init; } = Array.Empty<ActivitySession>();
    public IReadOnlyList<MonitoringTimeEntry> MonitoringEntries { get; private init; } = Array.Empty<MonitoringTimeEntry>();

    public static async Task<CaseloadSnapshot> LoadAsync(ProviderDashboardRepositories repos, ProviderContext context, BillingPeriod period, CancellationToken ct)
    {
        var provider = await repos.ProviderRepo.GetByIdAsync(context.ProviderId, ct)
            ?? throw new EntityNotFoundException("Provider not found.");
        var patients = await repos.PatientRepo.GetByProviderAsync(context.ProviderId, ct);
        var ids = patients.Select(p => p.Id).ToList();

        var historyStart = context.Today.AddDays(-HistoryDays).StartOfEasternDayUtc();
        var from = period.StartUtc < historyStart ? period.StartUtc : historyStart;
        var sessions = await repos.ActivitySessionRepo.GetByPatientsAsync(ids, from, context.NowUtc.AddMinutes(1), ct);
        var assessments = await repos.AssessmentRepo.GetByPatientsAsync(ids, ct);
        var communications = await repos.CareCommunicationRepo.GetByPatientsAsync(ids, period.StartUtc, period.EndUtcExclusive, ct);
        var monitoring = await repos.MonitoringTimeEntryRepo.GetByProviderAsync(context.ProviderId, period.StartUtc, period.EndUtcExclusive, ct);

        var sessionsByPatient = sessions.ToLookup(s => s.PatientId);
        var assessmentsByPatient = assessments.ToLookup(a => a.PatientId);
        var commsByPatient = communications.ToLookup(c => c.PatientId);
        var monitoringByPatient = monitoring.ToLookup(m => m.PatientId);

        var snapshots = patients
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => Build(p, sessionsByPatient[p.Id].ToList(), assessmentsByPatient[p.Id].ToList(),
                commsByPatient[p.Id].ToList(), monitoringByPatient[p.Id].ToList(), period, context.Today))
            .ToList();

        return new CaseloadSnapshot
        {
            Period = period,
            Today = context.Today,
            Provider = provider,
            Patients = snapshots,
            AllSessions = sessions,
            MonitoringEntries = monitoring
        };
    }

    public static RtmBillingSummary CalculateRtm(Patient patient, IEnumerable<ActivitySession> sessions, IEnumerable<CareCommunication> communications,
        IEnumerable<MonitoringTimeEntry> monitoring, BillingPeriod period, DateOnly today)
    {
        var inPeriodComms = communications.Where(c => period.Contains(c.OccurredAt.ToEasternDate()) && c.IsInteractive).ToList();
        return RtmBillingCalculator.Calculate(new RtmPeriodInput
        {
            Period = period,
            Today = today,
            RemoteDataDays = EngagementCalculator.RemoteDataDays(sessions.Where(s => period.Contains(s.StartedAt.ToEasternDate()))),
            MonitoringSeconds = monitoring.Where(m => period.Contains(m.StartedAt.ToEasternDate())).Sum(m => m.ActiveSeconds),
            InteractiveCommunicationCount = inPeriodComms.Count,
            InteractiveCommunicationMinutes = inPeriodComms.Sum(c => c.DurationMinutes),
            RtmConsentOn = patient.RtmConsentOn
        });
    }

    private static PatientSnapshot Build(Patient patient, List<ActivitySession> sessions, List<Assessment> assessments,
        List<CareCommunication> communications, List<MonitoringTimeEntry> monitoring, BillingPeriod period, DateOnly today)
    {
        var last30 = sessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-30)).ToList();
        var phq = assessments.Where(a => a.Instrument == AssessmentInstrument.Phq9).OrderBy(a => a.CompletedAt).ToList();
        var gad = assessments.Where(a => a.Instrument == AssessmentInstrument.Gad7).OrderBy(a => a.CompletedAt).ToList();
        var rtm = CalculateRtm(patient, sessions, communications, monitoring, period, today);
        var recent = sessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-60)).ToList();

        // Paused or discharged patients aren't expected to be active, so engagement alerts would only be noise.
        var attention = patient.Status == PatientStatus.Active
            ? AttentionRules.Evaluate(recent, assessments, period.Contains(today) ? rtm : null, today)
            : Array.Empty<AttentionItem>();

        return new PatientSnapshot
        {
            Patient = patient,
            Sessions = sessions,
            Assessments = assessments,
            Last30Days = EngagementCalculator.Summarize(last30),
            SessionsLast7Days = sessions.Count(s => s.StartedAt.ToEasternDate() > today.AddDays(-7)),
            DaysSinceLastSession = EngagementCalculator.DaysSinceLastSession(sessions, today),
            LatestPhq9 = phq.LastOrDefault(),
            PreviousPhq9 = phq.Count >= 2 ? phq[^2] : null,
            LatestGad7 = gad.LastOrDefault(),
            Rtm = rtm,
            Attention = attention
        };
    }
}
