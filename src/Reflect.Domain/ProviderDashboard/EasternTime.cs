namespace Reflect.Domain.ProviderDashboard;

/// <summary>Calendar-day math in US Eastern, which is how providers and payers count "days" and "months".</summary>
public static class EasternTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateOnly ToDate(DateTimeOffset utc) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Zone).DateTime);

    public static DateOnly Today(DateTimeOffset nowUtc) => ToDate(nowUtc);

    /// <summary>Local (US Eastern) hour of day, 0-23.</summary>
    public static int Hour(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone).Hour;

    /// <summary>Local (US Eastern) time of day.</summary>
    public static TimeOnly TimeOfDay(DateTimeOffset utc) => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, Zone).DateTime);

    /// <summary>UTC instant of local midnight at the start of <paramref name="day"/>.</summary>
    public static DateTimeOffset StartOfDayUtc(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }

    /// <summary>UTC instant for a local wall-clock time on <paramref name="day"/>.</summary>
    public static DateTimeOffset AtLocal(DateOnly day, TimeOnly time)
    {
        var local = day.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
