using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Domain.Tests;

public class MonitoringTimePolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 13, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Credits_reported_seconds_within_wall_clock()
    {
        var entry = new MonitoringTimeEntry { StartedAt = Start, LastActivityAt = Start };

        var credited = MonitoringTimePolicy.ApplyHeartbeat(entry, 15, Start.AddSeconds(15));

        Assert.Equal(15, credited);
        Assert.Equal(15, entry.ActiveSeconds);
    }

    [Fact]
    public void Never_credits_more_than_elapsed_time()
    {
        var entry = new MonitoringTimeEntry { StartedAt = Start, LastActivityAt = Start };

        var credited = MonitoringTimePolicy.ApplyHeartbeat(entry, 600, Start.AddSeconds(10));

        Assert.Equal(10 + MonitoringTimePolicy.ClockToleranceSeconds, credited);
    }

    [Fact]
    public void Caps_a_single_heartbeat()
    {
        var entry = new MonitoringTimeEntry { StartedAt = Start, LastActivityAt = Start };

        var credited = MonitoringTimePolicy.ApplyHeartbeat(entry, 300, Start.AddMinutes(10));

        Assert.Equal(MonitoringTimePolicy.MaxCreditPerHeartbeatSeconds, credited);
    }

    [Fact]
    public void Closed_entries_take_no_more_time()
    {
        var entry = new MonitoringTimeEntry { StartedAt = Start, LastActivityAt = Start, IsClosed = true, ActiveSeconds = 40 };

        Assert.Equal(0, MonitoringTimePolicy.ApplyHeartbeat(entry, 15, Start.AddSeconds(15)));
        Assert.Equal(40, entry.ActiveSeconds);
    }
}
