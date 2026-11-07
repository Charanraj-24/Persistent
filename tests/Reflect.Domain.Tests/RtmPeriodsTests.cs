using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.Tests;

public class RtmPeriodsTests
{
    private static readonly DateOnly Start = new(2026, 10, 1);

    [Theory]
    [InlineData("2026-10-01", "2026-10-01", "2026-10-30")]
    [InlineData("2026-10-30", "2026-10-01", "2026-10-30")]
    [InlineData("2026-10-31", "2026-10-31", "2026-11-29")]
    [InlineData("2026-12-15", "2026-11-30", "2026-12-29")]
    public void Device_periods_roll_every_30_days(string today, string expectedStart, string expectedEnd)
    {
        var p = RtmPeriods.DevicePeriodFor(Start, DateOnly.Parse(today));
        Assert.Equal(DateOnly.Parse(expectedStart), p.Start);
        Assert.Equal(DateOnly.Parse(expectedEnd), p.End);
        Assert.Equal(30, p.TotalDays);
    }

    [Fact]
    public void Before_start_returns_the_first_period()
    {
        var p = RtmPeriods.DevicePeriodFor(Start, Start.AddDays(-5));
        Assert.Equal(Start, p.Start);
    }

    [Fact]
    public void Calendar_month_handles_month_lengths()
    {
        var feb = RtmPeriods.CalendarMonthOf(new DateOnly(2028, 2, 10));
        Assert.Equal(new DateOnly(2028, 2, 1), feb.Start);
        Assert.Equal(new DateOnly(2028, 2, 29), feb.End);
    }

    [Theory]
    [InlineData("2026-10-01", 29)]
    [InlineData("2026-10-29", 1)]
    [InlineData("2026-10-30", 0)]
    [InlineData("2026-09-25", 30)]
    public void Days_remaining_counts_days_after_today(string today, int expected)
    {
        var p = new DateRange(Start, Start.AddDays(29));
        Assert.Equal(expected, p.DaysRemainingAfter(DateOnly.Parse(today)));
    }

    [Fact]
    public void Completed_periods_are_those_ended_before_today_within_the_range()
    {
        var october = new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));
        var rtmStart = new DateOnly(2026, 8, 2); // periods end 8/31, 9/30, 10/30, 11/29

        var asOfMonthEnd = RtmPeriods.CompletedPeriodsEndingIn(rtmStart, october, new DateOnly(2026, 11, 1));
        Assert.Single(asOfMonthEnd);
        Assert.Equal(new DateOnly(2026, 10, 30), asOfMonthEnd[0].End);

        // On 10/30 the period ending today is still open.
        Assert.Empty(RtmPeriods.CompletedPeriodsEndingIn(rtmStart, october, new DateOnly(2026, 10, 30)));
    }

    [Fact]
    public void Completed_periods_count_data_days_and_pick_a_code()
    {
        var rtmStart = new DateOnly(2026, 9, 1);
        var days = Enumerable.Range(0, 17).Select(i => rtmStart.AddDays(i)).ToList();
        var result = RtmBillingCalculator.CompletedPeriods(rtmStart, days, new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31)), new DateOnly(2026, 10, 15));
        var p = Assert.Single(result);
        Assert.Equal(17, p.DataDays);
        Assert.Equal("98978", p.Code);
    }
}
