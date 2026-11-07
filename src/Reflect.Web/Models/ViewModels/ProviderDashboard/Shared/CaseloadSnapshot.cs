using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

/// <summary>Everything a provider-level screen needs, loaded once. Read-only.</summary>
public sealed class CaseloadSnapshot
{
    public ProviderProfile Provider { get; private init; }
    public List<ProviderClient> Clients { get; private init; } = new();
    public List<AppSession> Sessions { get; private init; } = new();
    public List<MoodCheckIn> CheckIns { get; private init; } = new();
    public List<AssessmentResult> Assessments { get; private init; } = new();
    public List<RtmTimeEntry> TimeEntries { get; private init; } = new();
    public DateTimeOffset NowUtc { get; private init; }
    public DateOnly Today { get; private init; }

    public const int LookbackDays = 120;

    public static async Task<ProviderProfile> LoadProviderAsync(ICurrentUserService currentUser, IProviderProfileRepository providers, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new EntityNotFoundException("No signed-in user.");
        return await providers.GetByUserIdAsync(userId, ct);
    }

    public static async Task<CaseloadSnapshot> LoadAsync(
        ICurrentUserService currentUser,
        IProviderProfileRepository providers,
        IProviderClientRepository clients,
        IAppSessionRepository sessions,
        IMoodCheckInRepository checkIns,
        IAssessmentResultRepository assessments,
        IRtmTimeEntryRepository timeEntries,
        DateOnly? asOf,
        CancellationToken ct)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var realToday = EasternTime.Today(nowUtc);
        var today = asOf.HasValue && asOf.Value < realToday ? asOf.Value : realToday;
        var endUtc = today == realToday ? nowUtc : EasternTime.StartOfDayUtc(today.AddDays(1)).AddTicks(-1);
        var fromUtc = EasternTime.StartOfDayUtc(today.AddDays(-LookbackDays));

        var provider = await LoadProviderAsync(currentUser, providers, ct);
        var list = await clients.GetByProviderAsync(provider.Id, ct);
        var userIds = list.Select(c => c.ClientUserId).ToList();

        return new CaseloadSnapshot
        {
            Provider = provider,
            Clients = list.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList(),
            Sessions = await sessions.GetForClientsAsync(userIds, fromUtc, endUtc, ct),
            CheckIns = await checkIns.GetForClientsAsync(userIds, fromUtc, endUtc, ct),
            Assessments = await assessments.GetForClientsAsync(userIds, fromUtc, endUtc, ct),
            TimeEntries = await timeEntries.GetForProviderAsync(provider.Id, fromUtc, endUtc, ct),
            NowUtc = endUtc,
            Today = today
        };
    }

    public IEnumerable<AppSession> SessionsFor(ProviderClient c) => Sessions.Where(s => s.ClientUserId == c.ClientUserId);
    public IEnumerable<MoodCheckIn> CheckInsFor(ProviderClient c) => CheckIns.Where(s => s.ClientUserId == c.ClientUserId);
    public IEnumerable<AssessmentResult> AssessmentsFor(ProviderClient c) => Assessments.Where(s => s.ClientUserId == c.ClientUserId);
    public IEnumerable<RtmTimeEntry> TimeEntriesFor(ProviderClient c) => TimeEntries.Where(e => e.ClientId == c.Id);

    public RtmEvaluation EvaluateRtm(ProviderClient c) =>
        RtmClientEvaluator.Evaluate(c, SessionsFor(c), TimeEntriesFor(c), Today, EasternTime.ToDate);

    public EngagementSummary Summarize(ProviderClient c, int days = 30) =>
        EngagementAnalyzer.Summarize(
            SessionsFor(c).Where(s => s.StartedAt > NowUtc.AddDays(-days)).ToList(),
            CheckInsFor(c).Where(s => s.RecordedAt > NowUtc.AddDays(-days)).ToList(),
            NowUtc, EasternTime.ToDate);

    /// <summary>Flags use 30-day engagement but look at all assessments since the client's RTM baseline.</summary>
    public List<ClinicalFlag> FlagsFor(ProviderClient c)
    {
        var all = EngagementAnalyzer.Summarize(SessionsFor(c).ToList(), CheckInsFor(c).Where(s => s.RecordedAt > NowUtc.AddDays(-30)).ToList(), NowUtc, EasternTime.ToDate);
        return EngagementAnalyzer.Flags(all, AssessmentsFor(c).ToList(), NowUtc);
    }
}
