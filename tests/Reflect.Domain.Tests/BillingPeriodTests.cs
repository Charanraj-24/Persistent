using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Domain.Tests;

public class BillingPeriodTests
{
    [Fact]
    public void Month_period_covers_the_whole_calendar_month()
    {
        var feb = BillingPeriod.ForMonth(2028, 2);

        Assert.Equal(new DateOnly(2028, 2, 1), feb.Start);
        Assert.Equal(new DateOnly(2028, 2, 29), feb.End);
        Assert.Equal(29, feb.TotalDays);
        Assert.Equal("2028-02", feb.Key);
        Assert.Equal("February 2028", feb.Label);
    }

    [Fact]
    public void Utc_bounds_follow_us_eastern_midnight()
    {
        var oct = BillingPeriod.ForMonth(2026, 10);

        // EDT is UTC-4 on Oct 1; EST is UTC-5 on Nov 1.
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 4, 0, 0, TimeSpan.Zero), oct.StartUtc);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero), oct.EndUtcExclusive);
    }

    [Theory]
    [InlineData("2026-09", true)]
    [InlineData("2026-13", false)]
    [InlineData("", false)]
    [InlineData("september", false)]
    public void Parses_period_keys(string key, bool valid)
    {
        Assert.Equal(valid, BillingPeriod.TryParseKey(key, out _));
    }
}
