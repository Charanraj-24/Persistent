using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

/// <summary>Engagement tab: module mix, 12-week trend, time-of-day and weekday patterns, and the session log.</summary>
public class ClientEngagementTabViewModel
{
    public EngagementSummary Summary { get; set; }
    public string TopModuleName { get; set; } = string.Empty;
    public string LastSession { get; set; } = string.Empty;
    public string PeakTime { get; set; } = string.Empty;
    /// <summary>"Completed 9/12/2026", "Not completed" or "Not needed (no Reflect XR use)".</summary>
    public string Tutorial { get; set; } = string.Empty;
    public bool TutorialMissing { get; set; }
    public int LateNight14 { get; set; }
    public string LastLateNight { get; set; } = string.Empty;
    public List<SessionRowViewModel> Sessions { get; set; } = new();
    public string ChartDataJson { get; set; } = "{}";

    private static readonly (string Label, int From, int To)[] TimeBuckets =
    {
        ("Late night (12-5 AM)", 0, 5), ("Morning (5 AM-12)", 5, 12), ("Afternoon (12-5 PM)", 12, 17), ("Evening (5-9 PM)", 17, 21), ("Night (9 PM-12)", 21, 24)
    };

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessionsRepo, IMoodCheckInRepository checkInsRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var from = scope.DaysAgoUtc(90);
        var all = await sessionsRepo.GetForClientsAsync(scope.UserIds, scope.DaysAgoUtc(Shared.CaseloadSnapshot.LookbackDays), scope.NowUtc, ct);
        var sessions = all.Where(s => s.StartedAt >= from).ToList();
        var tutorial = all.Where(s => s.ModuleKey == ModuleCatalog.TutorialKey && s.Completed).OrderBy(s => s.StartedAt).FirstOrDefault();
        var usesXr = all.Any(s => s.Platform == ReflectPlatform.ReflectXR);
        Tutorial = tutorial != null ? "Completed " + Reflect.Domain.Helpers.DateTimeOffsetUsExtensions.ToUsDateString(tutorial.StartedAt)
            : usesXr ? "Not completed" : "Not needed yet";
        TutorialMissing = tutorial == null && usesXr;
        var checkIns = await checkInsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);

        var last30 = sessions.Where(s => s.StartedAt > scope.NowUtc.AddDays(-30)).ToList();
        Summary = EngagementAnalyzer.Summarize(last30, checkIns.Where(c => c.RecordedAt > scope.NowUtc.AddDays(-30)).ToList(), scope.NowUtc, EasternTime.ToDate);
        TopModuleName = string.IsNullOrEmpty(Summary.TopModuleKey) ? "None" : ModuleCatalog.Get(Summary.TopModuleKey).Name;
        LastSession = Summary.LastSessionAt.HasValue ? Reflect.Domain.Helpers.DateTimeOffsetUsExtensions.ToUsDateTimeString(Summary.LastSessionAt.Value) : "None";

        var bySession = checkIns.Where(c => c.AppSessionId.HasValue).GroupBy(c => c.AppSessionId.Value).ToDictionary(g => g.Key, g => g.ToList());
        Sessions = sessions.OrderByDescending(s => s.StartedAt)
            .Select(s => SessionRowViewModel.FromEntity(s, bySession.TryGetValue(s.Id, out var l) ? l : new List<MoodCheckIn>()))
            .ToList();

        // 12-week trend (minutes per week by app), weeks ending today.
        var weeks = Enumerable.Range(0, 12).Select(i => scope.Today.AddDays(-7 * (11 - i))).ToList();
        double WeekMinutes(DateOnly end, ReflectPlatform p) => Math.Round(sessions
            .Where(s => s.Platform == p)
            .Where(s => { var d = EasternTime.ToDate(s.StartedAt); return d > end.AddDays(-7) && d <= end; })
            .Sum(s => s.DurationMinutes));

        int LocalHour(AppSession s) => TimeZoneInfo.ConvertTime(s.StartedAt, TimeZoneInfo.FindSystemTimeZoneById("America/New_York")).Hour;
        var timeCounts = TimeBuckets.Select(b => last30.Count(s =>
        {
            var h = LocalHour(s);
            return h >= b.From && h < b.To;
        })).ToList();
        var full = EngagementAnalyzer.Summarize(sessions, Array.Empty<MoodCheckIn>(), scope.NowUtc, EasternTime.ToDate);
        LateNight14 = full.LateNightSessions14Days;
        LastLateNight = full.LastLateNightAt.HasValue ? Reflect.Domain.Helpers.DateTimeOffsetUsExtensions.ToUsDateTimeString(full.LastLateNightAt.Value) : string.Empty;
        PeakTime = timeCounts.Sum() == 0 ? "None" : TimeBuckets[timeCounts.IndexOf(timeCounts.Max())].Label;

        var weekdayNames = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
        var weekdayCounts = Enumerable.Range(0, 7).Select(i => last30.Count(s =>
            ((int)EasternTime.ToDate(s.StartedAt).DayOfWeek + 6) % 7 == i)).ToList();

        ChartDataJson = JsonSerializer.Serialize(new
        {
            modules = last30.GroupBy(s => s.ModuleKey)
                .Select(g => { var m = ModuleCatalog.Get(g.Key); return new { label = m.Name, group = m.GroupLabel, minutes = Math.Round(g.Sum(s => s.DurationMinutes)), sessions = g.Count(), xr = m.Platform == ReflectPlatform.ReflectXR }; })
                .OrderByDescending(m => m.minutes),
            groups = last30.GroupBy(s => ModuleCatalog.Get(s.ModuleKey).GroupLabel)
                .Select(g => new { label = g.Key, minutes = Math.Round(g.Sum(s => s.DurationMinutes)), sessions = g.Count(), xr = g.First().Platform == ReflectPlatform.ReflectXR })
                .OrderByDescending(m => m.minutes),
            weekly = new
            {
                labels = weeks.Select(w => "Wk of " + w.AddDays(-6).ToString("M/d", CultureInfo.InvariantCulture)),
                xr = weeks.Select(w => WeekMinutes(w, ReflectPlatform.ReflectXR)),
                mobile = weeks.Select(w => WeekMinutes(w, ReflectPlatform.ReflectMobile))
            },
            timeOfDay = new { labels = TimeBuckets.Select(b => b.Label), counts = timeCounts },
            weekday = new { labels = weekdayNames, counts = weekdayCounts }
        });
    }
}
