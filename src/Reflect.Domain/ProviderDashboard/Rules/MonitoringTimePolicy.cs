using Reflect.Domain.ProviderDashboard.Entities;

namespace Reflect.Domain.ProviderDashboard.Rules;

/// <summary>
/// Server-side guard for auto-tracked monitoring time. The browser reports active seconds; the server never credits
/// more than the wall-clock time since the last heartbeat, so a tampered or stuck client cannot inflate billable time.
/// </summary>
public static class MonitoringTimePolicy
{
    public const int HeartbeatIntervalSeconds = 15;
    public const int MaxCreditPerHeartbeatSeconds = 60;
    public const int ClockToleranceSeconds = 5;
    public const int MaxManualEntryMinutes = 120;

    /// <summary>Credits a heartbeat to an open entry and returns the seconds actually credited.</summary>
    public static int ApplyHeartbeat(MonitoringTimeEntry entry, int reportedActiveSeconds, DateTimeOffset now)
    {
        if (entry.IsClosed || reportedActiveSeconds <= 0)
        {
            if (!entry.IsClosed) entry.LastActivityAt = Max(entry.LastActivityAt, now);
            return 0;
        }

        var wallClock = (int)Math.Floor((now - entry.LastActivityAt).TotalSeconds) + ClockToleranceSeconds;
        var credited = Math.Min(reportedActiveSeconds, Math.Min(Math.Max(wallClock, 0), MaxCreditPerHeartbeatSeconds));
        entry.ActiveSeconds += credited;
        entry.LastActivityAt = Max(entry.LastActivityAt, now);
        return credited;
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
