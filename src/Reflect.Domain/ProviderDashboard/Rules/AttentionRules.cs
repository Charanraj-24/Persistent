using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Rules;

public sealed record AttentionItem(AttentionSeverity Severity, string Category, string Title, string Detail, string Icon);

/// <summary>Flags that tell a provider which patients to look at first. Each flag has a label and icon, never color alone.</summary>
public static class AttentionRules
{
    public const int InactiveWarningDays = 7;
    public const int InactiveCriticalDays = 14;
    public const int PhqSevereThreshold = 20;
    public const int MoodDropStreak = 3;

    public static IReadOnlyList<AttentionItem> Evaluate(
        IReadOnlyCollection<ActivitySession> recentSessions,
        IReadOnlyCollection<Assessment> assessments,
        RtmBillingSummary rtm,
        DateOnly today)
    {
        var items = new List<AttentionItem>();

        var daysInactive = EngagementCalculator.DaysSinceLastSession(recentSessions, today);
        if (daysInactive >= InactiveCriticalDays)
        {
            var detail = daysInactive == int.MaxValue ? "No sessions recorded yet." : $"Last session was {daysInactive} days ago.";
            items.Add(new AttentionItem(AttentionSeverity.Critical, "Engagement", "No recent activity", detail, "ri-time-line"));
        }
        else if (daysInactive >= InactiveWarningDays)
        {
            items.Add(new AttentionItem(AttentionSeverity.Warning, "Engagement", "Activity slowing", $"Last session was {daysInactive} days ago.", "ri-time-line"));
        }
        else
        {
            var last14 = CountBetween(recentSessions, today.AddDays(-13), today);
            var prior14 = CountBetween(recentSessions, today.AddDays(-27), today.AddDays(-14));
            if (prior14 >= 4 && last14 * 2 < prior14)
            {
                items.Add(new AttentionItem(AttentionSeverity.Warning, "Engagement", "Engagement dropped",
                    $"{last14} sessions in the last 14 days, down from {prior14}.", "ri-arrow-down-line"));
            }
        }

        foreach (var instrument in new[] { AssessmentInstrument.Phq9, AssessmentInstrument.Gad7 })
        {
            var ordered = assessments.Where(a => a.Instrument == instrument && !a.IsDeleted).OrderBy(a => a.CompletedAt).ToList();
            if (ordered.Count == 0) continue;
            var name = AssessmentSeverity.Name(instrument);
            var latest = ordered[^1];
            var meaningful = AssessmentSeverity.MeaningfulChange(instrument);

            if (instrument == AssessmentInstrument.Phq9 && latest.Score >= PhqSevereThreshold)
            {
                items.Add(new AttentionItem(AttentionSeverity.Critical, "Symptoms", $"{name} in severe range",
                    $"Latest score {latest.Score} on {latest.CompletedAt.ToUsDateString()}.", "ri-alarm-warning-line"));
            }
            if (ordered.Count >= 2)
            {
                var change = latest.Score - ordered[^2].Score;
                if (change >= meaningful)
                {
                    items.Add(new AttentionItem(AttentionSeverity.Critical, "Symptoms", $"{name} worsened",
                        $"Up {change} points ({ordered[^2].Score} to {latest.Score}).", "ri-arrow-up-line"));
                }
                var baseline = ordered[0];
                if (baseline.Score - latest.Score >= meaningful && change <= 0)
                {
                    items.Add(new AttentionItem(AttentionSeverity.Positive, "Symptoms", $"{name} improving",
                        $"Down {baseline.Score - latest.Score} points since baseline ({baseline.Score} to {latest.Score}).", "ri-arrow-down-line"));
                }
            }
        }

        var moodRated = recentSessions
            .Where(s => s.MoodBefore.HasValue && s.MoodAfter.HasValue)
            .OrderByDescending(s => s.StartedAt)
            .Take(MoodDropStreak)
            .ToList();
        if (moodRated.Count == MoodDropStreak && moodRated.All(s => s.MoodAfter < s.MoodBefore))
        {
            items.Add(new AttentionItem(AttentionSeverity.Warning, "Mood", "Mood lower after sessions",
                $"Mood dropped after each of the last {MoodDropStreak} rated sessions.", "ri-emotion-sad-line"));
        }

        if (rtm != null && rtm.Readiness != RtmReadiness.NotEnrolled)
        {
            if (rtm.TreatmentMinutes >= RtmBillingRules.ShortTreatmentMinutes && !rtm.HasInteractiveCommunication)
            {
                items.Add(new AttentionItem(AttentionSeverity.Warning, "Billing", "Interactive contact needed",
                    "Log a video or phone contact this month to bill treatment management.", "ri-phone-line"));
            }
            if (rtm.Readiness == RtmReadiness.AtRisk)
            {
                items.Add(new AttentionItem(AttentionSeverity.Info, "Billing", "Data days at risk",
                    $"{rtm.DataDays} remote data days; {rtm.DaysLeftInPeriod} days left cannot reach {rtm.DataDaysRequired}.", "ri-calendar-close-line"));
            }
            if (rtm.MinutesToNextThreshold is > 0 and <= 5 && rtm.NextThresholdMinutes.HasValue)
            {
                items.Add(new AttentionItem(AttentionSeverity.Info, "Billing", "Close to a time threshold",
                    $"{rtm.MinutesToNextThreshold} more minute(s) reaches {rtm.NextThresholdMinutes} minutes this month.", "ri-timer-line"));
            }
        }

        return items.OrderByDescending(i => i.Severity).ToList();
    }

    private static int CountBetween(IEnumerable<ActivitySession> sessions, DateOnly from, DateOnly to) =>
        sessions.Count(s =>
        {
            var day = s.StartedAt.ToEasternDate();
            return day >= from && day <= to;
        });
}
