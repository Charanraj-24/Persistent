using System.Globalization;
using System.Text;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.ProviderDashboard.Assistant;

/// <summary>Everything known about one client, already loaded. Inputs for <see cref="ClinicalContextBuilder"/>.</summary>
public sealed class ClientContextData
{
    public ProviderClient Client { get; init; }
    public string Token { get; init; } = "[CLIENT]";
    public IReadOnlyList<AppSession> Sessions { get; init; } = Array.Empty<AppSession>();
    public IReadOnlyList<MoodCheckIn> CheckIns { get; init; } = Array.Empty<MoodCheckIn>();
    public IReadOnlyList<AssessmentResult> Assessments { get; init; } = Array.Empty<AssessmentResult>();
    public IReadOnlyList<Observation> Observations { get; init; } = Array.Empty<Observation>();
    public RtmEvaluation Rtm { get; init; }
}

/// <summary>
/// Turns client data into compact, de-identified text for the AI. No names, MRNs, birthdates, payers or calendar
/// dates: people are tokens, ages are 10-year bands, and times are relative ("3 days ago", "evening").
/// </summary>
public static class ClinicalContextBuilder
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string ForClient(ClientContextData d, DateTimeOffset nowUtc, Func<DateTimeOffset, DateOnly> toLocalDate, Func<DateTimeOffset, int> toLocalHour) =>
        WithInvariantCulture(() => BuildClient(d, nowUtc, toLocalDate, toLocalHour));

    public static string ForCaseload(IReadOnlyList<ClientContextData> clients, DateTimeOffset nowUtc, Func<DateTimeOffset, DateOnly> toLocalDate, Func<DateTimeOffset, int> toLocalHour) =>
        WithInvariantCulture(() => BuildCaseload(clients, nowUtc, toLocalDate, toLocalHour));

    /// <summary>All numbers in the context are formatted with the invariant culture, whatever the server culture is.</summary>
    private static string WithInvariantCulture(Func<string> build)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = Inv;
        try { return build(); }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    private static string BuildClient(ClientContextData d, DateTimeOffset nowUtc, Func<DateTimeOffset, DateOnly> toLocalDate, Func<DateTimeOffset, int> toLocalHour)
    {
        var sb = new StringBuilder();
        var today = toLocalDate(nowUtc);
        AppendHeader(sb, d, today);
        sb.AppendLine();
        AppendClientDetail(sb, d, nowUtc, today, toLocalDate, toLocalHour, detailed: true);
        return sb.ToString().TrimEnd();
    }

    private static string BuildCaseload(IReadOnlyList<ClientContextData> clients, DateTimeOffset nowUtc, Func<DateTimeOffset, DateOnly> toLocalDate, Func<DateTimeOffset, int> toLocalHour)
    {
        var sb = new StringBuilder();
        var today = toLocalDate(nowUtc);
        sb.AppendLine("## Summary");
        sb.AppendLine($"- Caseload: {clients.Count} clients on remote therapeutic monitoring (tokens below).");
        sb.AppendLine($"- Billable now: {clients.Count(c => c.Rtm.Status == RtmBillingStatus.Billable)}; action needed: {clients.Count(c => c.Rtm.Status == RtmBillingStatus.Blocked)}.");
        sb.AppendLine($"- Days left in the month: {RtmPeriods.CalendarMonthOf(today).End.DayNumber - today.DayNumber}.");
        foreach (var c in clients)
        {
            sb.AppendLine();
            AppendHeader(sb, c, today);
            AppendClientDetail(sb, c, nowUtc, today, toLocalDate, toLocalHour, detailed: false);
        }
        return sb.ToString().TrimEnd();
    }

    private static void AppendHeader(StringBuilder sb, ClientContextData d, DateOnly today)
    {
        var c = d.Client;
        var age = today.Year - c.DateOfBirth.Year - (today < c.DateOfBirth.AddYears(today.Year - c.DateOfBirth.Year) ? 1 : 0);
        sb.AppendLine($"## Client {d.Token}");
        sb.AppendLine($"- Age band: {age / 10 * 10}-{age / 10 * 10 + 9}");
        sb.AppendLine($"- Primary diagnosis: {c.PrimaryDiagnosisCode} {c.PrimaryDiagnosisLabel}");
        sb.AppendLine($"- Treatment goals: {c.TreatmentGoals}");
        sb.AppendLine($"- Monitoring: {c.Status}; RTM consent on file: {(c.RtmConsentDate.HasValue ? "yes" : "no")}; monitored for {Days(today.DayNumber - c.RtmStartDate.DayNumber)}.");
    }

    private static void AppendClientDetail(StringBuilder sb, ClientContextData d, DateTimeOffset nowUtc, DateOnly today,
        Func<DateTimeOffset, DateOnly> toLocalDate, Func<DateTimeOffset, int> toLocalHour, bool detailed)
    {
        string Ago(DateTimeOffset t) => DaysAgo(today.DayNumber - toLocalDate(t).DayNumber);

        var last30 = d.Sessions.Where(s => s.StartedAt > nowUtc.AddDays(-30)).ToList();
        var checks30 = d.CheckIns.Where(c => c.RecordedAt > nowUtc.AddDays(-30)).ToList();
        var summary = EngagementAnalyzer.Summarize(last30, checks30, nowUtc, toLocalDate);
        var all = EngagementAnalyzer.Summarize(d.Sessions.ToList(), checks30, nowUtc, toLocalDate);
        var flags = EngagementAnalyzer.Flags(all, d.Assessments.ToList(), nowUtc);

        if (detailed) sb.AppendLine("## Summary");
        sb.AppendLine($"- Last 30 days: {summary.Sessions} sessions on {summary.ActiveDays} active days, {summary.TotalMinutes:0} min " +
                      $"(Reflect XR {summary.XrShare * 100:0}%, Mobile {(1 - summary.XrShare) * 100:0}%); avg session {summary.AverageSessionMinutes:0} min; completion {summary.CompletionRate * 100:0}%.");
        sb.AppendLine($"- Sessions in the last 14 days: {all.SessionsLast14Days} (prior 14 days: {all.SessionsPrior14Days}).");
        sb.AppendLine($"- Last session: {(all.LastSessionAt.HasValue ? Ago(all.LastSessionAt.Value) : "none recorded")}.");
        if (!string.IsNullOrEmpty(summary.TopModuleKey)) sb.AppendLine($"- Most used module (30 days): {ModuleCatalog.Get(summary.TopModuleKey).Name}.");
        if (summary.AverageMoodChange.HasValue)
            sb.AppendLine($"- Self-rated change per session (30 days, 1-10 scales): mood {Signed(summary.AverageMoodChange.Value)}, stress {Signed(summary.AverageStressChange ?? 0)}.");

        foreach (var inst in new[] { AssessmentInstrument.Phq9, AssessmentInstrument.Gad7 })
        {
            var series = d.Assessments.Where(a => a.Instrument == inst).OrderBy(a => a.CompletedAt).ToList();
            if (series.Count == 0) continue;
            var latest = series[^1];
            var name = AssessmentScoring.Name(inst);
            var line = $"- Latest {name}: {latest.TotalScore}/{AssessmentScoring.MaxScore(inst)} ({AssessmentScoring.Severity(inst, latest.TotalScore).ToLowerInvariant()}), {Ago(latest.CompletedAt)}";
            if (series.Count > 1) line += $"; change since first in window: {Signed(latest.TotalScore - series[0].TotalScore)}";
            if (inst == AssessmentInstrument.Phq9) line += latest.SafetyItemScore > 0 ? $"; ITEM 9 (self-harm thoughts) ENDORSED = {latest.SafetyItemScore}" : "; item 9 not endorsed";
            sb.AppendLine(line + ".");
        }
        sb.AppendLine(flags.Count == 0 ? "- Flags: none." : "- Flags: " + string.Join("; ", flags.Select(f => $"{f.SeverityLabel}: {f.Title} ({f.Detail})")));

        var r = d.Rtm;
        sb.AppendLine($"- RTM: {r.DataDaysInPeriod} data days this 30-day period ({r.DaysRemainingInPeriod} days left); {r.ManagementMinutes} attested provider minutes this month" +
                      $"{(r.PendingMinutes > 0 ? $" (+{r.PendingMinutes} pending)" : string.Empty)}; live interaction this month: {(r.HasRealTimeInteraction ? "yes" : "no")}; " +
                      $"codes supported now: {(r.Codes.Count == 0 ? "none" : string.Join(", ", r.Codes.Select(c => c.Units > 1 ? $"{c.Code} x{c.Units}" : c.Code)))}" +
                      $"{(r.Blockers.Count > 0 ? "; blockers: " + string.Join(" ", r.Blockers) : string.Empty)}.");

        if (!detailed) return;

        sb.AppendLine();
        sb.AppendLine("## Minutes by module (last 30 days)");
        foreach (var g in last30.GroupBy(s => s.ModuleKey).OrderByDescending(g => g.Sum(s => s.DurationMinutes)))
            sb.AppendLine($"- {ModuleLabel(g.Key)}: {g.Sum(s => s.DurationMinutes):0} min, {Count(g.Count(), "session")}");

        sb.AppendLine();
        sb.AppendLine("## Minutes per week (oldest first)");
        for (var w = 7; w >= 0; w--)
        {
            var end = today.AddDays(-7 * w);
            var inWeek = d.Sessions.Where(s => { var day = toLocalDate(s.StartedAt); return day > end.AddDays(-7) && day <= end; }).ToList();
            var label = w == 0 ? "this week (last 7 days)" : $"{w} {(w == 1 ? "week" : "weeks")} ago";
            sb.AppendLine($"- {label}: {Count(inWeek.Count, "session")}, {inWeek.Sum(s => s.DurationMinutes):0} min");
        }

        var assess = d.Assessments.OrderBy(a => a.CompletedAt).ToList();
        if (assess.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Assessments (oldest first)");
            foreach (var a in assess)
                sb.AppendLine($"- {AssessmentScoring.Name(a.Instrument)} {Ago(a.CompletedAt)}: {a.TotalScore} ({AssessmentScoring.Severity(a.Instrument, a.TotalScore).ToLowerInvariant()}){(a.SafetyItemScore > 0 ? ", item 9 endorsed" : string.Empty)}");
        }

        var bySession = d.CheckIns.Where(c => c.AppSessionId.HasValue).GroupBy(c => c.AppSessionId.Value).ToDictionary(g => g.Key, g => g.ToList());
        sb.AppendLine();
        sb.AppendLine("## Recent sessions (newest first, up to 15)");
        foreach (var s in d.Sessions.OrderByDescending(s => s.StartedAt).Take(15))
        {
            var line = $"- {Ago(s.StartedAt)}, {PartOfDay(toLocalHour(s.StartedAt))}: {ModuleLabel(s.ModuleKey)}, {s.DurationMinutes:0} min, {(s.Completed ? "completed" : "not completed")}";
            if (bySession.TryGetValue(s.Id, out var cs))
            {
                var pre = cs.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession);
                var post = cs.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession);
                if (pre != null && post != null) line += $", mood {pre.MoodScore} to {post.MoodScore}, stress {pre.StressScore} to {post.StressScore}";
                if (post != null && !string.IsNullOrEmpty(post.EmotionTags)) line += $", emotion words: {post.EmotionTags.Replace(",", ", ")}";
            }
            sb.AppendLine(line);
        }

        var tags = checks30.Where(c => !string.IsNullOrWhiteSpace(c.EmotionTags))
            .SelectMany(c => c.EmotionTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t).OrderByDescending(g => g.Count()).Take(8).ToList();
        if (tags.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Emotion words chosen after sessions (last 30 days)");
            sb.AppendLine("- " + string.Join(", ", tags.Select(t => $"{t.Key} ({t.Count()})")));
        }

        var obs = d.Observations.Where(o => o.EffectiveAt > nowUtc.AddDays(-30)).GroupBy(o => o.Code).ToList();
        if (obs.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Physiologic readings (last 30 days)");
            foreach (var g in obs)
            {
                var info = ObservationCatalog.Get(g.Key);
                var pre = g.Where(o => o.Context == "pre").ToList();
                var post = g.Where(o => o.Context == "post").ToList();
                sb.AppendLine($"- {info.Display}: {g.Count()} readings; latest {g.OrderByDescending(o => o.EffectiveAt).First().Value:0} {info.Unit}" +
                              (pre.Count > 0 && post.Count > 0 ? $"; average before sessions {pre.Average(o => o.Value):0}, after {post.Average(o => o.Value):0}" : string.Empty));
            }
        }

        var points = TalkingPointsBuilder.Build(d.Sessions, d.CheckIns, d.Assessments, nowUtc);
        if (points.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## System-generated talking points");
            foreach (var p in points) sb.AppendLine("- " + p);
        }
    }

    private static string Count(int n, string noun) => $"{n} {noun}{(n == 1 ? "" : "s")}";

    /// <summary>Module name plus platform, without doubling a platform already in the name.</summary>
    private static string ModuleLabel(string key)
    {
        var m = ModuleCatalog.Get(key);
        return $"{m.Name} ({(m.Platform == ReflectPlatform.ReflectXR ? "XR" : "Mobile")}, {m.Group})";
    }

    private static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.0", Inv);
    private static string Signed(int v) => (v >= 0 ? "+" : "") + v.ToString(Inv);
    private static string Days(int n) => n == 1 ? "1 day" : $"{n} days";
    private static string DaysAgo(int n) => n <= 0 ? "today" : n == 1 ? "yesterday" : $"{n} days ago";
    private static string PartOfDay(int hour) => hour < 6 ? "night" : hour < 12 ? "morning" : hour < 17 ? "afternoon" : hour < 21 ? "evening" : "night";
}
