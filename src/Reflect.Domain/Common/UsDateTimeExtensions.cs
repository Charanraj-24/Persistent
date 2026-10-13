using System.Globalization;

namespace Reflect.Domain.Common;

// Mirrors the real app's US date helpers: M/d/yyyy and M/d/yyyy h:mm tt, converted to US Eastern.
public static class UsDateTimeExtensions
{
    public static readonly TimeZoneInfo UsEastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateTimeOffset ToEastern(this DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, UsEastern);

    public static DateOnly ToEasternDate(this DateTimeOffset value) => DateOnly.FromDateTime(value.ToEastern().DateTime);

    public static string ToUsDateString(this DateTimeOffset value) =>
        value.ToEastern().ToString("M/d/yyyy", CultureInfo.InvariantCulture);

    public static string ToUsDateTimeString(this DateTimeOffset value) =>
        value.ToEastern().ToString("M/d/yyyy h:mm tt", CultureInfo.InvariantCulture);

    public static string ToUsTimeString(this DateTimeOffset value) =>
        value.ToEastern().ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string ToUsDateString(this DateOnly value) =>
        value.ToString("M/d/yyyy", CultureInfo.InvariantCulture);

    /// <summary>Start of the given Eastern calendar day, as a UTC instant.</summary>
    public static DateTimeOffset StartOfEasternDayUtc(this DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, UsEastern.GetUtcOffset(local)).ToUniversalTime();
    }
}
