using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Domain.Tests;

public class RtmBillingCalculatorTests
{
    private static readonly BillingPeriod October = BillingPeriod.ForMonth(2026, 10);

    private static IReadOnlyCollection<DateOnly> Days(int count, int startDay = 1) =>
        Enumerable.Range(startDay, count).Select(d => new DateOnly(2026, 10, d)).ToList();

    private static RtmPeriodInput Input(int dataDays, int monitoringMinutes, int interactiveCount = 1, int interactiveMinutes = 0,
        DateOnly? consent = null, DateOnly? today = null) => new()
    {
        Period = October,
        Today = today ?? new DateOnly(2026, 11, 2),
        RemoteDataDays = Days(dataDays),
        MonitoringSeconds = monitoringMinutes * 60,
        InteractiveCommunicationCount = interactiveCount,
        InteractiveCommunicationMinutes = interactiveMinutes,
        RtmConsentOn = consent ?? new DateOnly(2026, 6, 1)
    };

    [Fact]
    public void Full_month_with_20_minutes_and_interactive_contact_is_ready()
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 18, monitoringMinutes: 22));

        Assert.Equal(RtmReadiness.Ready, result.Readiness);
        Assert.Contains(result.Codes, c => c.Code == "98978");
        Assert.Contains(result.Codes, c => c.Code == "98980" && c.Units == 1);
        Assert.DoesNotContain(result.Codes, c => c.Code == "98981");
    }

    [Theory]
    [InlineData(39, 0)]
    [InlineData(40, 1)]
    [InlineData(59, 1)]
    [InlineData(60, 2)]
    public void Each_additional_full_20_minutes_adds_a_98981_unit(int minutes, int expectedUnits)
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 20, monitoringMinutes: minutes));

        var units = result.Codes.Where(c => c.Code == "98981").Sum(c => c.Units);
        Assert.Equal(expectedUnits, units);
    }

    [Fact]
    public void Ten_to_nineteen_minutes_bills_the_short_management_code()
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 16, monitoringMinutes: 14));

        Assert.Contains(result.Codes, c => c.Code == "98979");
        Assert.DoesNotContain(result.Codes, c => c.Code == "98980");
        Assert.Equal(RtmReadiness.Partial, result.Readiness);
    }

    [Fact]
    public void Interactive_communication_minutes_count_toward_treatment_time()
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 16, monitoringMinutes: 12, interactiveMinutes: 8));

        Assert.Equal(20, result.TreatmentMinutes);
        Assert.Contains(result.Codes, c => c.Code == "98980");
    }

    [Fact]
    public void Treatment_codes_need_an_interactive_communication()
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 20, monitoringMinutes: 30, interactiveCount: 0));

        Assert.DoesNotContain(result.Codes, c => c.Code is "98979" or "98980" or "98981");
        Assert.Contains(result.Blockers, b => b.Contains("interactive", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(1, null)]
    [InlineData(2, "98986")]
    [InlineData(15, "98986")]
    [InlineData(16, "98978")]
    public void Device_supply_code_depends_on_remote_data_days(int dataDays, string expected)
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays, monitoringMinutes: 0));

        var supply = result.Codes.FirstOrDefault(c => c.Code is "98978" or "98986")?.Code;
        Assert.Equal(expected, supply);
    }

    [Fact]
    public void Initial_setup_is_offered_only_in_the_consent_month()
    {
        var consentInPeriod = RtmBillingCalculator.Calculate(Input(dataDays: 5, monitoringMinutes: 0, consent: new DateOnly(2026, 10, 3)));
        var consentEarlier = RtmBillingCalculator.Calculate(Input(dataDays: 5, monitoringMinutes: 0));

        Assert.Contains(consentInPeriod.Codes, c => c.Code == "98975");
        Assert.DoesNotContain(consentEarlier.Codes, c => c.Code == "98975");
    }

    [Fact]
    public void Patient_without_consent_is_not_enrolled_and_has_no_codes()
    {
        var input = new RtmPeriodInput { Period = October, Today = new DateOnly(2026, 10, 20), RemoteDataDays = Days(20), MonitoringSeconds = 3600, InteractiveCommunicationCount = 1 };

        var result = RtmBillingCalculator.Calculate(input);

        Assert.Equal(RtmReadiness.NotEnrolled, result.Readiness);
        Assert.Empty(result.Codes);
    }

    [Fact]
    public void Open_period_that_can_no_longer_reach_16_days_is_at_risk()
    {
        // 5 data days on Oct 1-5, today Oct 25: at most 5 + 7 open days = 12.
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 5, monitoringMinutes: 25, today: new DateOnly(2026, 10, 25)));

        Assert.Equal(RtmReadiness.AtRisk, result.Readiness);
        Assert.False(result.CanStillReachFullDataDays);
        Assert.Equal(7, result.DaysLeftInPeriod);
    }

    [Fact]
    public void Open_period_that_can_still_reach_16_days_is_on_track()
    {
        var result = RtmBillingCalculator.Calculate(Input(dataDays: 5, monitoringMinutes: 5, today: new DateOnly(2026, 10, 10)));

        Assert.Equal(RtmReadiness.OnTrack, result.Readiness);
        Assert.True(result.CanStillReachFullDataDays);
    }

    [Fact]
    public void Data_days_outside_the_period_are_ignored()
    {
        var input = new RtmPeriodInput
        {
            Period = October,
            Today = new DateOnly(2026, 11, 2),
            RemoteDataDays = new[] { new DateOnly(2026, 9, 30), new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1) },
            RtmConsentOn = new DateOnly(2026, 1, 1)
        };

        Assert.Equal(1, RtmBillingCalculator.Calculate(input).DataDays);
    }

    [Fact]
    public void Open_period_that_needs_a_session_most_remaining_days_is_behind_pace()
    {
        // 2 data days, today Oct 13: 19 open days, 14 still needed (74% would be fine, 14/19 = 0.737 is on track).
        var onTrack = RtmBillingCalculator.Calculate(Input(dataDays: 2, monitoringMinutes: 0, today: new DateOnly(2026, 10, 13)));
        // 0 data days, today Oct 13: 16 of 19 open days needed = 84%.
        var behind = RtmBillingCalculator.Calculate(Input(dataDays: 0, monitoringMinutes: 0, today: new DateOnly(2026, 10, 13)));

        Assert.Equal(RtmReadiness.OnTrack, onTrack.Readiness);
        Assert.Equal(RtmReadiness.BehindPace, behind.Readiness);
    }

    [Theory]
    [InlineData(0, 10, 10)]
    [InlineData(9, 10, 1)]
    [InlineData(10, 20, 10)]
    [InlineData(20, 40, 20)]
    [InlineData(41, 60, 19)]
    public void Next_threshold_follows_10_20_40_60(int minutes, int threshold, int toGo)
    {
        var (next, remaining) = RtmBillingCalculator.NextThreshold(minutes);

        Assert.Equal(threshold, next);
        Assert.Equal(toGo, remaining);
    }
}
