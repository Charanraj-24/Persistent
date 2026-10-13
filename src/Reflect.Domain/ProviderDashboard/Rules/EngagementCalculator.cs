using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Catalogs;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Rules;

public sealed record ModuleUsage(string ModuleKey, string ModuleName, ReflectPlatform Platform, int Sessions, int Minutes);

public sealed record WeeklyEngagement(DateOnly WeekStart, int XrMinutes, int MobileMinutes, int Sessions, int ActiveDays);

public sealed class EngagementSummary
{
    public int Sessions { get; init; }
    public int TotalMinutes { get; init; }
    public int AverageMinutes { get; init; }
    public int ActiveDays { get; init; }
    public int XrSessions { get; init; }
    public int MobileSessions { get; init; }
    public int XrMinutes { get; init; }
    public int MobileMinutes { get; init; }
    public int RemoteSessions { get; init; }
    public int InClinicSessions { get; init; }
    public int CompletedSessions { get; init; }
    public int ReflectionsWritten { get; init; }
    public int Creations { get; init; }
    /// <summary>Average of (mood after - mood before) across sessions with both ratings. Null when none.</summary>
    public decimal? AverageMoodChange { get; init; }
    public DateTimeOffset? LastSessionAt { get; init; }
    public IReadOnlyList<ModuleUsage> Modules { get; init; } = Array.Empty<ModuleUsage>();

    public int XrMinutesPercent => TotalMinutes == 0 ? 0 : (int)Math.Round(XrMinutes * 100.0 / TotalMinutes);
    public int CompletionPercent => Sessions == 0 ? 0 : (int)Math.Round(CompletedSessions * 100.0 / Sessions);
}

public static class EngagementCalculator
{
    public static EngagementSummary Summarize(IEnumerable<ActivitySession> sessions)
    {
        var list = sessions.Where(s => !s.IsDeleted).ToList();
        var total = list.Sum(s => s.DurationMinutes);
        var moodPairs = list.Where(s => s.MoodBefore.HasValue && s.MoodAfter.HasValue).ToList();

        return new EngagementSummary
        {
            Sessions = list.Count,
            TotalMinutes = total,
            AverageMinutes = list.Count == 0 ? 0 : (int)Math.Round((double)total / list.Count),
            ActiveDays = list.Select(s => s.StartedAt.ToEasternDate()).Distinct().Count(),
            XrSessions = list.Count(s => s.Platform == ReflectPlatform.ReflectXR),
            MobileSessions = list.Count(s => s.Platform == ReflectPlatform.ReflectMobile),
            XrMinutes = list.Where(s => s.Platform == ReflectPlatform.ReflectXR).Sum(s => s.DurationMinutes),
            MobileMinutes = list.Where(s => s.Platform == ReflectPlatform.ReflectMobile).Sum(s => s.DurationMinutes),
            RemoteSessions = list.Count(s => s.Setting == SessionSetting.Remote),
            InClinicSessions = list.Count(s => s.Setting == SessionSetting.InClinic),
            CompletedSessions = list.Count(s => s.Completed),
            ReflectionsWritten = list.Count(s => s.ReflectionWritten),
            Creations = list.Sum(s => s.CreationsCount),
            AverageMoodChange = moodPairs.Count == 0
                ? null
                : Math.Round((decimal)moodPairs.Average(s => s.MoodAfter.Value - s.MoodBefore.Value), 1),
            LastSessionAt = list.Count == 0 ? null : list.Max(s => s.StartedAt),
            Modules = list
                .GroupBy(s => s.ModuleKey)
                .Select(g =>
                {
                    var module = ModuleCatalog.Get(g.Key);
                    return new ModuleUsage(g.Key, module.Name, module.Platform, g.Count(), g.Sum(s => s.DurationMinutes));
                })
                .OrderByDescending(m => m.Minutes)
                .ToList()
        };
    }

    /// <summary>Monday-start weeks ending with the week that contains <paramref name="today"/>, oldest first.</summary>
    public static IReadOnlyList<WeeklyEngagement> Weekly(IEnumerable<ActivitySession> sessions, DateOnly today, int weeks)
    {
        var currentWeekStart = WeekStart(today);
        var byWeek = sessions
            .Where(s => !s.IsDeleted)
            .GroupBy(s => WeekStart(s.StartedAt.ToEasternDate()))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<WeeklyEngagement>();
        for (var i = weeks - 1; i >= 0; i--)
        {
            var start = currentWeekStart.AddDays(-7 * i);
            byWeek.TryGetValue(start, out var items);
            items ??= new List<ActivitySession>();
            result.Add(new WeeklyEngagement(
                start,
                items.Where(s => s.Platform == ReflectPlatform.ReflectXR).Sum(s => s.DurationMinutes),
                items.Where(s => s.Platform == ReflectPlatform.ReflectMobile).Sum(s => s.DurationMinutes),
                items.Count,
                items.Select(s => s.StartedAt.ToEasternDate()).Distinct().Count()));
        }
        return result;
    }

    /// <summary>Distinct Eastern days with at least one remote session. Only these count as RTM data days.</summary>
    public static IReadOnlyCollection<DateOnly> RemoteDataDays(IEnumerable<ActivitySession> sessions) =>
        sessions
            .Where(s => !s.IsDeleted && s.Setting == SessionSetting.Remote)
            .Select(s => s.StartedAt.ToEasternDate())
            .Distinct()
            .ToList();

    public static DateOnly WeekStart(DateOnly day)
    {
        var offset = ((int)day.DayOfWeek + 6) % 7; // Monday = 0
        return day.AddDays(-offset);
    }

    public static int DaysSinceLastSession(IEnumerable<ActivitySession> sessions, DateOnly today)
    {
        var last = sessions.Where(s => !s.IsDeleted).Select(s => (DateOnly?)s.StartedAt.ToEasternDate()).Max();
        return last.HasValue ? today.DayNumber - last.Value.DayNumber : int.MaxValue;
    }
}
