using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard;

namespace Reflect.Domain.ProviderDashboard.Clinical;

public enum FlagSeverity
{
    Critical = 1,
    Warning = 2,
    Info = 3,
    Positive = 4
}

public sealed record ClinicalFlag(FlagSeverity Severity, string Title, string Detail, string Icon)
{
    public string Color => Severity switch
    {
        FlagSeverity.Critical => "danger",
        FlagSeverity.Warning => "warning",
        FlagSeverity.Positive => "success",
        _ => "info"
    };

    public string SeverityLabel => Severity switch
    {
        FlagSeverity.Critical => "Urgent",
        FlagSeverity.Warning => "Review",
        FlagSeverity.Positive => "Progress",
        _ => "Note"
    };
}

public sealed class EngagementSummary
{
    public int Sessions { get; init; }
    public double TotalMinutes { get; init; }
    public double XrMinutes { get; init; }
    public double MobileMinutes { get; init; }
    public int ActiveDays { get; init; }
    public double AverageSessionMinutes { get; init; }
    public double CompletionRate { get; init; }
    public string TopModuleKey { get; init; } = string.Empty;
    public DateTimeOffset? LastSessionAt { get; init; }
    public int SessionsLast14Days { get; init; }
    public int SessionsPrior14Days { get; init; }
    public double? AverageMoodChange { get; init; }
    public double? AverageStressChange { get; init; }
    public int DaysSinceLastSession { get; init; }
    /// <summary>Reflect XR sessions in the window (tutorial included).</summary>
    public int XrSessions { get; init; }
    /// <summary>When the client first completed the XR Training Tutorial, if they have.</summary>
    public DateTimeOffset? TutorialCompletedAt { get; init; }
    /// <summary>Sessions started between midnight and 5 AM (Eastern) in the last 14 days.</summary>
    public int LateNightSessions14Days { get; init; }
    public DateTimeOffset? LastLateNightAt { get; init; }

    public double XrShare => TotalMinutes <= 0 ? 0 : XrMinutes / TotalMinutes;
}

/// <summary>Turns raw sessions, check-ins and assessments into a summary and clinical flags. Pure and testable.</summary>
public static class EngagementAnalyzer
{
    public const int InactivityDays = 7;
    public const double DropThreshold = 0.5;
    public const int MinPriorSessionsForDrop = 4;
    /// <summary>Late night = a session starting from midnight up to (not including) this hour, local time.</summary>
    public const int LateNightEndHour = 5;
    public const int LateNightFlagThreshold = 2;

    public static bool IsLateNight(DateTimeOffset startedAtUtc) => EasternTime.Hour(startedAtUtc) < LateNightEndHour;

    public static EngagementSummary Summarize(IReadOnlyCollection<AppSession> sessions, IReadOnlyCollection<MoodCheckIn> checkIns, DateTimeOffset nowUtc, Func<DateTimeOffset, DateOnly> toLocalDate)
    {
        var list = sessions.Where(s => !s.IsDeleted && s.StartedAt <= nowUtc).ToList();
        var last = list.Count == 0 ? (DateTimeOffset?)null : list.Max(s => s.StartedAt);
        var total = list.Sum(s => s.DurationMinutes);

        var top = list.GroupBy(s => s.ModuleKey)
            .OrderByDescending(g => g.Sum(s => s.DurationMinutes))
            .Select(g => g.Key)
            .FirstOrDefault() ?? string.Empty;

        // Mood/stress change: pair each post-session check-in with the pre-session check-in of the same session.
        var bySession = checkIns.Where(c => c.AppSessionId.HasValue && !c.IsDeleted)
            .GroupBy(c => c.AppSessionId.Value)
            .Select(g => new
            {
                Pre = g.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession),
                Post = g.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession)
            })
            .Where(p => p.Pre != null && p.Post != null)
            .ToList();

        var lateNight = list.Where(s => s.StartedAt > nowUtc.AddDays(-14) && IsLateNight(s.StartedAt)).ToList();
        var tutorial = list.Where(s => s.ModuleKey == ModuleCatalog.TutorialKey && s.Completed).OrderBy(s => s.StartedAt).FirstOrDefault();

        return new EngagementSummary
        {
            XrSessions = list.Count(s => s.Platform == ReflectPlatform.ReflectXR),
            TutorialCompletedAt = tutorial?.StartedAt,
            LateNightSessions14Days = lateNight.Count,
            LastLateNightAt = lateNight.Count == 0 ? null : lateNight.Max(s => s.StartedAt),
            Sessions = list.Count,
            TotalMinutes = total,
            XrMinutes = list.Where(s => s.Platform == ReflectPlatform.ReflectXR).Sum(s => s.DurationMinutes),
            MobileMinutes = list.Where(s => s.Platform == ReflectPlatform.ReflectMobile).Sum(s => s.DurationMinutes),
            ActiveDays = list.Select(s => toLocalDate(s.StartedAt)).Distinct().Count(),
            AverageSessionMinutes = list.Count == 0 ? 0 : total / list.Count,
            CompletionRate = list.Count == 0 ? 0 : list.Count(s => s.Completed) / (double)list.Count,
            TopModuleKey = top,
            LastSessionAt = last,
            SessionsLast14Days = list.Count(s => s.StartedAt > nowUtc.AddDays(-14)),
            SessionsPrior14Days = list.Count(s => s.StartedAt <= nowUtc.AddDays(-14) && s.StartedAt > nowUtc.AddDays(-28)),
            AverageMoodChange = bySession.Count == 0 ? null : bySession.Average(p => p.Post.MoodScore - p.Pre.MoodScore),
            AverageStressChange = bySession.Count == 0 ? null : bySession.Average(p => p.Post.StressScore - p.Pre.StressScore),
            DaysSinceLastSession = last.HasValue ? (int)Math.Floor((nowUtc - last.Value).TotalDays) : int.MaxValue
        };
    }

    public static List<ClinicalFlag> Flags(EngagementSummary summary, IReadOnlyCollection<AssessmentResult> assessments, DateTimeOffset nowUtc)
    {
        var flags = new List<ClinicalFlag>();
        var ordered = assessments.Where(a => !a.IsDeleted).OrderBy(a => a.CompletedAt).ToList();

        // Safety first: PHQ-9 item 9 endorsed on the most recent PHQ-9.
        var latestPhq = ordered.LastOrDefault(a => a.Instrument == AssessmentInstrument.Phq9);
        if (latestPhq != null && latestPhq.SafetyItemScore > 0)
        {
            flags.Add(new ClinicalFlag(FlagSeverity.Critical, "PHQ-9 item 9 endorsed",
                $"Most recent PHQ-9 reports thoughts of self-harm (item 9 = {latestPhq.SafetyItemScore}). Follow your safety protocol.",
                "ri-alarm-warning-line"));
        }

        foreach (var instrument in new[] { AssessmentInstrument.Phq9, AssessmentInstrument.Gad7 })
        {
            var series = ordered.Where(a => a.Instrument == instrument).ToList();
            if (series.Count < 2) continue;
            var change = series[^1].TotalScore - series[0].TotalScore;
            var name = AssessmentScoring.Name(instrument);
            if (change >= AssessmentScoring.MeaningfulChange)
                flags.Add(new ClinicalFlag(FlagSeverity.Warning, $"{name} worsening",
                    $"{name} rose {change} points ({series[0].TotalScore} to {series[^1].TotalScore}) since baseline.", "ri-arrow-up-line"));
            else if (change <= -AssessmentScoring.MeaningfulChange)
                flags.Add(new ClinicalFlag(FlagSeverity.Positive, $"{name} improving",
                    $"{name} dropped {-change} points ({series[0].TotalScore} to {series[^1].TotalScore}) since baseline.", "ri-arrow-down-line"));
        }

        if (summary.DaysSinceLastSession >= InactivityDays)
        {
            var detail = summary.LastSessionAt.HasValue
                ? $"No Reflect activity in {summary.DaysSinceLastSession} days."
                : "No Reflect activity recorded yet.";
            flags.Add(new ClinicalFlag(FlagSeverity.Warning, "Inactive", detail, "ri-time-line"));
        }
        else if (summary.SessionsPrior14Days >= MinPriorSessionsForDrop &&
                 summary.SessionsLast14Days <= summary.SessionsPrior14Days * (1 - DropThreshold))
        {
            flags.Add(new ClinicalFlag(FlagSeverity.Warning, "Engagement dropping",
                $"{summary.SessionsLast14Days} sessions in the last 14 days, down from {summary.SessionsPrior14Days}.", "ri-line-chart-line"));
        }

        if (summary.LateNightSessions14Days >= LateNightFlagThreshold)
        {
            flags.Add(new ClinicalFlag(FlagSeverity.Warning, "Late-night use",
                $"{summary.LateNightSessions14Days} sessions between midnight and 5 AM in the last 14 days, latest at {EasternTime.TimeOfDay(summary.LastLateNightAt!.Value):h:mm tt}. Ask about sleep and night-time worry.",
                "ri-moon-clear-line"));
        }

        if (summary.XrSessions > 0 && summary.TutorialCompletedAt == null)
        {
            flags.Add(new ClinicalFlag(FlagSeverity.Info, "Training Tutorial not completed",
                "Uses Reflect XR but hasn't completed the Training Tutorial. Check they know how to use the headset and modules.",
                "ri-graduation-cap-line"));
        }

        if (summary.AverageMoodChange is >= 1.0)
        {
            flags.Add(new ClinicalFlag(FlagSeverity.Positive, "Sessions lift mood",
                $"Mood rises {summary.AverageMoodChange:0.0} points on average after a session.", "ri-emotion-happy-line"));
        }

        return flags.OrderBy(f => f.Severity).ToList();
    }
}
