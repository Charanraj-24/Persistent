using System.Globalization;
using System.Text;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;

namespace Reflect.Domain.ProviderDashboard.Assistant;

/// <summary>
/// Drafts an RTM treatment-management note from the data, without AI (always available, deterministic).
/// The provider edits and attests it. Refers to the person as "Client" so it can also be sent to an AI for polishing.
/// </summary>
public static class RtmNoteDrafter
{
    public const string AiInstruction =
        "Rewrite the draft below as a concise RTM treatment-management note for the clinical record (4-6 sentences, plain " +
        "text, no headings, no bullets). Keep every number exactly as given and add nothing that isn't in the data. " +
        "Cover: what monitoring data was reviewed, engagement, therapeutic response, any safety concern, and the plan. " +
        "Refer to the person as \"Client\". Do not add diagnoses or medication advice.\n\nDRAFT:\n";

    public static string Draft(ClientContextData d, DateTimeOffset nowUtc, Func<DateTimeOffset, DateOnly> toLocalDate)
    {
        var inv = CultureInfo.InvariantCulture;
        var last30 = d.Sessions.Where(s => s.StartedAt > nowUtc.AddDays(-30)).ToList();
        var checks30 = d.CheckIns.Where(c => c.RecordedAt > nowUtc.AddDays(-30)).ToList();
        var s30 = EngagementAnalyzer.Summarize(last30, checks30, nowUtc, toLocalDate);
        var all = EngagementAnalyzer.Summarize(d.Sessions.ToList(), checks30, nowUtc, toLocalDate);
        var flags = EngagementAnalyzer.Flags(all, d.Assessments.ToList(), nowUtc);
        var r = d.Rtm;
        var sb = new StringBuilder();

        sb.Append(string.Create(inv, $"Reviewed Reflect remote therapeutic monitoring data for the last 30 days: {s30.Sessions} {(s30.Sessions == 1 ? "session" : "sessions")} ({s30.TotalMinutes:0} min) on {s30.ActiveDays} {(s30.ActiveDays == 1 ? "day" : "days")}, {s30.XrShare * 100:0}% in Reflect XR."));
        if (!string.IsNullOrEmpty(s30.TopModuleKey)) sb.Append($" Most used module: {ModuleCatalog.Get(s30.TopModuleKey).Name}.");
        if (s30.AverageMoodChange.HasValue)
            sb.Append(string.Create(inv, $" Self-rated mood changed {Signed(s30.AverageMoodChange.Value)} and stress {Signed(s30.AverageStressChange ?? 0)} per session on average."));

        foreach (var inst in new[] { AssessmentInstrument.Phq9, AssessmentInstrument.Gad7 })
        {
            var series = d.Assessments.Where(a => a.Instrument == inst).OrderBy(a => a.CompletedAt).ToList();
            if (series.Count == 0) continue;
            var latest = series[^1];
            sb.Append(string.Create(inv, $" Latest {AssessmentScoring.Name(inst)} {latest.TotalScore} ({AssessmentScoring.Severity(inst, latest.TotalScore).ToLowerInvariant()})"));
            if (series.Count > 1) sb.Append(string.Create(inv, $", {Signed(latest.TotalScore - series[0].TotalScore)} since baseline"));
            sb.Append('.');
        }

        var safety = flags.FirstOrDefault(f => f.Severity == FlagSeverity.Critical);
        if (safety != null) sb.Append(" PHQ-9 item 9 endorsed; safety assessed per clinic protocol.");
        var concerns = flags.Where(f => f.Severity == FlagSeverity.Warning).Select(f => f.Title).ToList();
        if (concerns.Count > 0) sb.Append($" Noted: {string.Join(", ", concerns)}.");

        var recentTop = d.Sessions.Where(x => x.StartedAt > nowUtc.AddDays(-7)).GroupBy(x => x.ModuleKey)
            .OrderByDescending(g => g.Sum(x => x.DurationMinutes)).Select(g => ModuleCatalog.Get(g.Key).Name).FirstOrDefault();
        sb.Append(" Plan: continue Reflect home practice and review progress at the next session");
        sb.Append(recentTop == null ? ", including barriers to recent use." : $", including recent work in {recentTop}.");
        return sb.ToString();
    }

    private static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.0", CultureInfo.InvariantCulture);
    private static string Signed(int v) => (v >= 0 ? "+" : "") + v.ToString(CultureInfo.InvariantCulture);
    private static string LowerFirst(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
