using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.Tests;

public class RtmBillingCalculatorTests
{
    private static readonly DateOnly PeriodStart = new(2026, 10, 1);
    private static readonly DateRange Period = new(PeriodStart, PeriodStart.AddDays(29));
    private static readonly DateRange October = new(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31));

    private static RtmEvaluation Evaluate(int dataDays = 0, double minutes = 0, bool live = true, bool consent = true,
        bool setupBilled = true, DateOnly? today = null, double pending = 0)
    {
        return RtmBillingCalculator.Evaluate(new RtmEvaluationInput
        {
            Today = today ?? new DateOnly(2026, 10, 30),
            DevicePeriod = Period,
            ManagementMonth = October,
            DataDays = Enumerable.Range(0, dataDays).Select(i => PeriodStart.AddDays(i)).ToList(),
            AttestedMinutes = minutes,
            PendingMinutes = pending,
            HasRealTimeInteraction = live,
            HasConsent = consent,
            SetupBilled = setupBilled
        });
    }

    [Theory]
    [InlineData(0, "")]
    [InlineData(1, "")]
    [InlineData(2, "98986")]
    [InlineData(15, "98986")]
    [InlineData(16, "98978")]
    [InlineData(30, "98978")]
    public void Device_code_follows_data_day_tiers(int days, string expected)
    {
        var e = Evaluate(dataDays: days);
        Assert.Equal(expected, e.DeviceCode);
        Assert.Equal(days, e.DataDaysInPeriod);
    }

    [Fact]
    public void Device_tiers_are_never_billed_together()
    {
        var e = Evaluate(dataDays: 20);
        Assert.True(e.HasCode("98978"));
        Assert.False(e.HasCode("98986"));
    }

    [Fact]
    public void Days_outside_the_period_and_in_the_future_are_ignored()
    {
        var input = new RtmEvaluationInput
        {
            Today = new DateOnly(2026, 10, 10),
            DevicePeriod = Period,
            ManagementMonth = October,
            DataDays = new[]
            {
                new DateOnly(2026, 9, 30),  // before period
                new DateOnly(2026, 10, 2),  // counts
                new DateOnly(2026, 10, 2),  // duplicate
                new DateOnly(2026, 10, 9),  // counts
                new DateOnly(2026, 10, 20)  // future
            },
            HasConsent = true,
            SetupBilled = true
        };
        var e = RtmBillingCalculator.Evaluate(input);
        Assert.Equal(2, e.DataDaysInPeriod);
        Assert.Equal("98986", e.DeviceCode);
    }

    [Theory]
    [InlineData(0, new string[0], "98979", 10)]
    [InlineData(9.99, new string[0], "98979", 1)]
    [InlineData(10, new[] { "98979" }, "98980", 10)]
    [InlineData(19.99, new[] { "98979" }, "98980", 1)]
    [InlineData(20, new[] { "98980" }, "98981", 20)]
    [InlineData(39, new[] { "98980" }, "98981", 1)]
    [InlineData(40, new[] { "98980", "98981" }, "98981", 20)]
    [InlineData(65, new[] { "98980", "98981" }, "98981", 15)]
    public void Management_codes_follow_minute_thresholds_without_rounding(double minutes, string[] expectedCodes, string next, int toNext)
    {
        var e = Evaluate(dataDays: 0, minutes: minutes);
        var mgmt = e.Codes.Select(c => c.Code).Where(c => c.StartsWith("9898") || c == "98979").ToArray();
        Assert.Equal(expectedCodes, mgmt);
        Assert.Equal(next, e.NextManagementCode);
        Assert.Equal(toNext, e.MinutesToNextManagementCode);
    }

    [Theory]
    [InlineData(40, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 2)]
    [InlineData(80, 3)]
    public void Additional_20_minute_blocks_are_counted_as_units(double minutes, int units)
    {
        var e = Evaluate(minutes: minutes);
        Assert.Equal(units, e.Codes.Single(c => c.Code == "98981").Units);
    }

    [Fact]
    public void Management_codes_need_a_live_interaction()
    {
        var e = Evaluate(dataDays: 18, minutes: 25, live: false);
        Assert.False(e.HasCode("98980"));
        Assert.True(e.HasCode("98978"));
        Assert.Single(e.Blockers);
        Assert.Equal(RtmBillingStatus.Billable, e.Status);
    }

    [Fact]
    public void Nothing_is_billable_without_consent()
    {
        var e = Evaluate(dataDays: 18, minutes: 25, consent: false);
        Assert.Empty(e.Codes);
        Assert.Equal(RtmBillingStatus.Blocked, e.Status);
        Assert.Contains(e.Blockers, b => b.Contains("consent"));
    }

    [Fact]
    public void Setup_code_is_offered_once_after_two_days()
    {
        Assert.True(Evaluate(dataDays: 2, setupBilled: false).HasCode("98975"));
        Assert.False(Evaluate(dataDays: 1, setupBilled: false).HasCode("98975"));
        Assert.False(Evaluate(dataDays: 5, setupBilled: true).HasCode("98975"));
    }

    [Fact]
    public void Next_tier_is_unreachable_when_too_few_days_remain()
    {
        // 14 data days, today is day 29 of 30: only 1 day left, 2 needed.
        var e = Evaluate(dataDays: 14, today: PeriodStart.AddDays(28));
        Assert.Equal(2, e.DataDaysToNextTier);
        Assert.Equal(1, e.DaysRemainingInPeriod);
        Assert.False(e.NextTierReachable);
    }

    [Fact]
    public void Pending_minutes_are_reported_but_not_billed()
    {
        var e = Evaluate(minutes: 15, pending: 10);
        Assert.Equal(15, e.ManagementMinutes);
        Assert.Equal(10, e.PendingMinutes);
        Assert.True(e.HasCode("98979"));
        Assert.False(e.HasCode("98980"));
    }

    [Fact]
    public void Empty_month_is_not_yet_billable()
    {
        var e = Evaluate();
        Assert.Equal(RtmBillingStatus.NotYetBillable, e.Status);
        Assert.Empty(e.Codes);
    }

    [Fact]
    public void Codes_for_month_use_completed_periods_and_management_codes_only()
    {
        var current = Evaluate(dataDays: 18, minutes: 22, setupBilled: false);
        var completed = new[]
        {
            new CompletedDevicePeriod(new DateRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)), 12, "98986")
        };
        var codes = RtmClientEvaluator.CodesForMonth(current, completed, hasConsent: true).Select(c => c.Code).ToList();
        Assert.Equal(new[] { "98986", "98975", "98980" }, codes);
        Assert.DoesNotContain("98978", codes); // current period is still open
        Assert.Empty(RtmClientEvaluator.CodesForMonth(current, completed, hasConsent: false));
    }
}
