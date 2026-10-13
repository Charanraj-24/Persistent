using System.Globalization;
using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Catalogs;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Rules;

public sealed record Insight(string Icon, string Text);

/// <summary>
/// Plain-language observations a provider can bring into session. Rule-based and deterministic, so every sentence
/// can be traced back to the data shown on the page.
/// </summary>
public static class InsightBuilder
{
    public static IReadOnlyList<Insight> Build(
        IReadOnlyCollection<ActivitySession> sessions,
        IReadOnlyCollection<Assessment> assessments,
        IReadOnlyCollection<PhysiologicReading> readings,
        DateOnly today)
    {
        var insights = new List<Insight>();
        var c = CultureInfo.InvariantCulture;

        var last28 = sessions.Where(s => s.StartedAt.ToEasternDate() > today.AddDays(-28)).ToList();
        var prior28 = sessions.Where(s => s.StartedAt.ToEasternDate() <= today.AddDays(-28) && s.StartedAt.ToEasternDate() > today.AddDays(-56)).ToList();
        var recent = EngagementCalculator.Summarize(last28);
        var before = EngagementCalculator.Summarize(prior28);

        if (recent.Sessions > 0 || before.Sessions > 0)
        {
            var trend = recent.ActiveDays > before.ActiveDays ? "up from" : recent.ActiveDays < before.ActiveDays ? "down from" : "the same as";
            insights.Add(new Insight("ri-calendar-check-line",
                $"Active on {recent.ActiveDays} of the last 28 days, {trend} {before.ActiveDays} in the 28 days before."));
        }

        if (recent.TotalMinutes > 0)
        {
            var top = recent.Modules.FirstOrDefault();
            var xrShare = recent.XrMinutesPercent;
            var platform = xrShare >= 60 ? $"Mostly uses Reflect XR ({xrShare}% of minutes)"
                : xrShare <= 40 ? $"Mostly uses Reflect Mobile ({100 - xrShare}% of minutes)"
                : $"Splits time between Reflect XR and Reflect Mobile ({xrShare}% XR)";
            var module = top == null ? "." : $"; {top.ModuleName} ({ModuleCatalog.PlatformName(top.Platform)}) is the most-used module.";
            insights.Add(new Insight("ri-device-line", platform + module));
        }

        if (recent.AverageMoodChange.HasValue)
        {
            var change = recent.AverageMoodChange.Value;
            var text = change > 0
                ? $"Self-rated mood rises by {change.ToString("0.0", c)} points on average after a session."
                : change < 0
                    ? $"Self-rated mood falls by {Math.Abs(change).ToString("0.0", c)} points on average after a session. Worth exploring in session."
                    : "Self-rated mood is unchanged on average after sessions.";
            insights.Add(new Insight(change >= 0 ? "ri-emotion-happy-line" : "ri-emotion-unhappy-line", text));
        }

        var phq = assessments.Where(a => a.Instrument == AssessmentInstrument.Phq9).OrderBy(a => a.CompletedAt).ToList();
        if (phq.Count >= 2)
        {
            var first = phq[0];
            var last = phq[^1];
            var fromBand = AssessmentSeverity.Classify(AssessmentInstrument.Phq9, first.Score).Label.ToLowerInvariant();
            var toBand = AssessmentSeverity.Classify(AssessmentInstrument.Phq9, last.Score).Label.ToLowerInvariant();
            var verb = last.Score < first.Score ? "fell" : last.Score > first.Score ? "rose" : "held steady";
            insights.Add(new Insight("ri-line-chart-line",
                $"PHQ-9 {verb} from {first.Score} ({fromBand}) on {first.CompletedAt.ToUsDateString()} to {last.Score} ({toBand}) on {last.CompletedAt.ToUsDateString()}."));
        }

        var heartRate = readings.Where(r => r.MetricCode == PhysiologicMetricCatalog.HeartRate && r.ActivitySessionId.HasValue).ToList();
        var deltas = heartRate
            .GroupBy(r => r.ActivitySessionId.Value)
            .Select(g => new { Start = g.FirstOrDefault(r => r.Context == "start"), End = g.FirstOrDefault(r => r.Context == "end") })
            .Where(x => x.Start != null && x.End != null)
            .Select(x => x.End.Value - x.Start.Value)
            .ToList();
        if (deltas.Count >= 3)
        {
            var avg = Math.Round(deltas.Average(), 1);
            var text = avg < 0
                ? $"Heart rate drops {Math.Abs(avg).ToString("0.#", c)} bpm on average from the start to the end of VR sessions ({deltas.Count} sessions)."
                : $"Heart rate rises {avg.ToString("0.#", c)} bpm on average during VR sessions ({deltas.Count} sessions).";
            insights.Add(new Insight("ri-heart-pulse-line", text));
        }

        return insights;
    }
}
