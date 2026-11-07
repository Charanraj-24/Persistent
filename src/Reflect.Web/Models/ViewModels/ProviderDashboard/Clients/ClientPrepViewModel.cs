using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

/// <summary>Session prep: one focused screen to read in the minutes before a session. Viewing it counts as review time.</summary>
public class ClientPrepViewModel
{
    /// <summary>Latest notes, pinned first (filled by the controller).</summary>
    public List<Reflect.Web.Models.ViewModels.ProviderDashboard.Notes.NoteRowViewModel> RecentNotes { get; set; } = new();
    public int NoteTotal { get; set; }

    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string DiagnosisLabel { get; set; } = string.Empty;
    public bool IsPaused { get; set; }
    public string TrackingKey { get; set; } = string.Empty;

    public List<string> TalkingPoints { get; set; } = new();
    public List<ClinicalFlag> Flags { get; set; } = new();
    public List<AssessmentRowViewModel> LatestScores { get; set; } = new();
    public List<SessionRowViewModel> ThisWeek { get; set; } = new();
    public int SessionsThisWeek { get; set; }
    public int MinutesThisWeek { get; set; }
    public string LastSession { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;
    public BadgeViewModel RtmBadge { get; set; }
    public string RtmNextStep { get; set; } = string.Empty;
    public bool HasLiveInteractionThisMonth { get; set; }
    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessionsRepo, IMoodCheckInRepository checkInsRepo, IAssessmentResultRepository assessmentsRepo,
        IRtmTimeEntryRepository timeEntriesRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var c = scope.Client;
        var from = scope.DaysAgoUtc(CaseloadSnapshot.LookbackDays);
        var month = RtmPeriods.CalendarMonthOf(scope.Today);
        var sessions = await sessionsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var checkIns = await checkInsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var assessments = await assessmentsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var entries = await timeEntriesRepo.GetForClientAsync(c.Id, EasternTime.StartOfDayUtc(month.Start), scope.NowUtc, ct);

        ClientId = c.Id;
        Name = c.FullName;
        FirstName = c.FirstName;
        Initials = c.Initials;
        DiagnosisLabel = c.PrimaryDiagnosisLabel;
        IsPaused = c.Status == ClientRtmStatus.Paused;
        Goals = c.TreatmentGoals;
        TrackingKey = Guid.NewGuid().ToString("N");

        TalkingPoints = TalkingPointsBuilder.Build(sessions, checkIns, assessments, scope.NowUtc, 5);
        var summary = EngagementAnalyzer.Summarize(sessions, checkIns.Where(x => x.RecordedAt > scope.NowUtc.AddDays(-30)).ToList(), scope.NowUtc, EasternTime.ToDate);
        Flags = EngagementAnalyzer.Flags(summary, assessments, scope.NowUtc);
        LastSession = summary.LastSessionAt.HasValue ? summary.LastSessionAt.Value.ToUsDateTimeString() : "No sessions yet";

        foreach (var inst in new[] { AssessmentInstrument.Phq9, AssessmentInstrument.Gad7 })
        {
            var series = assessments.Where(a => a.Instrument == inst).OrderBy(a => a.CompletedAt).ToList();
            if (series.Count > 0) LatestScores.Add(AssessmentRowViewModel.FromEntity(series[^1], series.Count > 1 ? series[0] : null));
        }

        var weekAgo = scope.NowUtc.AddDays(-7);
        var bySession = checkIns.Where(x => x.AppSessionId.HasValue).GroupBy(x => x.AppSessionId.Value).ToDictionary(g => g.Key, g => g.ToList());
        var week = sessions.Where(s => s.StartedAt > weekAgo).OrderByDescending(s => s.StartedAt).ToList();
        SessionsThisWeek = week.Count;
        MinutesThisWeek = (int)Math.Round(week.Sum(s => s.DurationMinutes));
        ThisWeek = week.Take(8).Select(s => SessionRowViewModel.FromEntity(s, bySession.TryGetValue(s.Id, out var l) ? l : new List<MoodCheckIn>())).ToList();

        var rtm = RtmClientEvaluator.Evaluate(c, sessions, entries, scope.Today, EasternTime.ToDate);
        RtmBadge = RtmStatusPresenter.StatusBadge(rtm);
        RtmNextStep = RtmStatusPresenter.NextStep(rtm);
        HasLiveInteractionThisMonth = rtm.HasRealTimeInteraction;

        var pairs = checkIns.Where(x => x.AppSessionId.HasValue && x.RecordedAt > scope.NowUtc.AddDays(-21)).GroupBy(x => x.AppSessionId.Value)
            .Select(g => new { Pre = g.FirstOrDefault(x => x.Phase == CheckInPhase.PreSession), Post = g.FirstOrDefault(x => x.Phase == CheckInPhase.PostSession) })
            .Where(p => p.Pre != null && p.Post != null).OrderBy(p => p.Pre.RecordedAt).ToList();
        ChartDataJson = JsonSerializer.Serialize(new
        {
            mood = new
            {
                labels = pairs.Select(p => p.Pre.RecordedAt.ToEastern().ToString("M/d", CultureInfo.InvariantCulture)),
                pre = pairs.Select(p => p.Pre.MoodScore),
                post = pairs.Select(p => p.Post.MoodScore)
            }
        });
    }
}
