namespace Reflect.Domain.ProviderDashboard.Rtm;

public readonly record struct DateRange(DateOnly Start, DateOnly End)
{
    public int TotalDays => End.DayNumber - Start.DayNumber + 1;
    public bool Contains(DateOnly day) => day >= Start && day <= End;
    public int DaysRemainingAfter(DateOnly today) => today >= End ? 0 : End.DayNumber - Math.Max(today.DayNumber, Start.DayNumber - 1);
}

public static class RtmPeriods
{
    /// <summary>
    /// The rolling 30-day device-supply period containing <paramref name="today"/>,
    /// counted from the client's RTM start date.
    /// </summary>
    public static DateRange DevicePeriodFor(DateOnly rtmStart, DateOnly today)
    {
        if (today < rtmStart)
            return new DateRange(rtmStart, rtmStart.AddDays(RtmBillingRules.DevicePeriodDays - 1));

        var index = (today.DayNumber - rtmStart.DayNumber) / RtmBillingRules.DevicePeriodDays;
        var start = rtmStart.AddDays(index * RtmBillingRules.DevicePeriodDays);
        return new DateRange(start, start.AddDays(RtmBillingRules.DevicePeriodDays - 1));
    }

    /// <summary>
    /// Device-supply periods that are complete (ended before <paramref name="today"/>) and whose last day
    /// falls inside <paramref name="range"/>. These are the periods that can be billed in that range.
    /// </summary>
    public static List<DateRange> CompletedPeriodsEndingIn(DateOnly rtmStart, DateRange range, DateOnly today)
    {
        var result = new List<DateRange>();
        var start = rtmStart;
        while (true)
        {
            var period = new DateRange(start, start.AddDays(RtmBillingRules.DevicePeriodDays - 1));
            if (period.End > range.End || period.End >= today) break;
            if (period.End >= range.Start) result.Add(period);
            start = start.AddDays(RtmBillingRules.DevicePeriodDays);
        }
        return result;
    }

    /// <summary>The calendar month containing <paramref name="day"/> (treatment-management codes are per calendar month).</summary>
    public static DateRange CalendarMonthOf(DateOnly day)
    {
        var start = new DateOnly(day.Year, day.Month, 1);
        return new DateRange(start, start.AddMonths(1).AddDays(-1));
    }
}
