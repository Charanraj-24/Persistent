using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Domain.ProviderDashboard.Clinical;

/// <summary>
/// Plain-language prompts a provider can bring into the next therapy session, for example
/// "Spent 34 minutes in Explore Emotions this week and tagged 'grief' twice."
/// This is the "I wouldn't have known that had I not looked at the dashboard" moment from the sponsor kickoff.
/// </summary>
public static class TalkingPointsBuilder
{
    public static List<string> Build(
        IReadOnlyCollection<AppSession> sessions,
        IReadOnlyCollection<MoodCheckIn> checkIns,
        IReadOnlyCollection<AssessmentResult> assessments,
        DateTimeOffset nowUtc,
        int max = 4)
    {
        var points = new List<string>();
        var weekAgo = nowUtc.AddDays(-7);

        // Safety always comes first.
        var latestPhq = assessments.Where(a => a.Instrument == AssessmentInstrument.Phq9).OrderByDescending(a => a.CompletedAt).FirstOrDefault();
        if (latestPhq != null && latestPhq.SafetyItemScore > 0)
        {
            var daysAgo = (int)Math.Floor((nowUtc - latestPhq.CompletedAt).TotalDays);
            points.Add($"Start with safety: PHQ-9 item 9 was endorsed {(daysAgo <= 0 ? "today" : daysAgo == 1 ? "yesterday" : $"{daysAgo} days ago")}. Complete a risk assessment per your protocol.");
        }
        var recent = sessions.Where(s => s.StartedAt > weekAgo && s.StartedAt <= nowUtc).ToList();

        // Late-night use (for example 3 AM) can point to insomnia or night-time worry.
        var lateNight = sessions.Where(s => s.StartedAt > nowUtc.AddDays(-14) && s.StartedAt <= nowUtc && EngagementAnalyzer.IsLateNight(s.StartedAt))
            .OrderByDescending(s => s.StartedAt).ToList();
        if (lateNight.Count >= EngagementAnalyzer.LateNightFlagThreshold)
        {
            var nights = lateNight.Select(s => EasternTime.ToDate(s.StartedAt)).Distinct().Count();
            points.Add($"Used Reflect after midnight on {nights} {(nights == 1 ? "night" : "nights")} in the last 2 weeks (latest {EasternTime.TimeOfDay(lateNight[0].StartedAt):h:mm tt}, {ModuleCatalog.Get(lateNight[0].ModuleKey).Name}). Ask about sleep and what keeps them up.");
        }

        // New to VR and hasn't done the Training Tutorial.
        if (sessions.Any(s => s.Platform == ReflectPlatform.ReflectXR) && !sessions.Any(s => s.ModuleKey == ModuleCatalog.TutorialKey && s.Completed))
            points.Add("Hasn't completed the Reflect XR Training Tutorial. Check they're comfortable with the headset and how to start a module.");

        if (recent.Count > 0)
        {
            var topModule = recent.GroupBy(s => s.ModuleKey)
                .Select(g => new { Key = g.Key, Minutes = g.Sum(s => s.DurationMinutes), Count = g.Count() })
                .OrderByDescending(g => g.Minutes)
                .First();
            var name = ModuleCatalog.Get(topModule.Key).Name;
            points.Add(string.Create(CultureInfo.InvariantCulture,
                $"Spent {topModule.Minutes:0} minutes in {name} over the past week ({topModule.Count} {(topModule.Count == 1 ? "session" : "sessions")}). Ask what drew them to it."));
        }

        var tags = checkIns.Where(c => c.RecordedAt > nowUtc.AddDays(-14) && !string.IsNullOrWhiteSpace(c.EmotionTags))
            .SelectMany(c => c.EmotionTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(2)
            .ToList();
        if (tags.Count > 0)
        {
            var phrase = string.Join(" and ", tags.Select(t => $"\"{t.Tag}\" ({t.Count}x)"));
            points.Add($"Most chosen emotion words in the last 2 weeks: {phrase}.");
        }

        // Session with the biggest mood lift.
        var pairs = checkIns.Where(c => c.AppSessionId.HasValue && c.RecordedAt > nowUtc.AddDays(-14))
            .GroupBy(c => c.AppSessionId.Value)
            .Select(g => new
            {
                SessionId = g.Key,
                Pre = g.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession),
                Post = g.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession)
            })
            .Where(p => p.Pre != null && p.Post != null)
            .Select(p => new { p.SessionId, Lift = p.Post.MoodScore - p.Pre.MoodScore, p.Pre, p.Post })
            .OrderByDescending(p => p.Lift)
            .ToList();
        var best = pairs.FirstOrDefault();
        if (best != null && best.Lift >= 2)
        {
            var session = sessions.FirstOrDefault(s => s.Id == best.SessionId);
            if (session != null)
            {
                points.Add($"Biggest mood lift: {ModuleCatalog.Get(session.ModuleKey).Name} took mood from {best.Pre.MoodScore} to {best.Post.MoodScore}. Explore what helped.");
            }
        }
        var worst = pairs.LastOrDefault();
        if (worst != null && worst.Lift <= -2)
        {
            var session = sessions.FirstOrDefault(s => s.Id == worst.SessionId);
            if (session != null)
                points.Add($"Mood dropped from {worst.Pre.MoodScore} to {worst.Post.MoodScore} after {ModuleCatalog.Get(session.ModuleKey).Name}. It may have surfaced difficult material.");
        }

        var latest = assessments.OrderByDescending(a => a.CompletedAt).FirstOrDefault();
        if (latest != null && latest.CompletedAt > nowUtc.AddDays(-14))
        {
            var name = AssessmentScoring.Name(latest.Instrument);
            points.Add($"Latest {name}: {latest.TotalScore} ({AssessmentScoring.Severity(latest.Instrument, latest.TotalScore).ToLowerInvariant()}). Review it together.");
        }

        if (recent.Count == 0)
            points.Add("No Reflect activity this week. Ask about barriers (time, headset access, motivation).");

        return points.Take(max).ToList();
    }
}
