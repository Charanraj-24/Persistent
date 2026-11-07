// STAND-IN: matches the real app's date helpers described in the tech spec
// (ToUsDateString => M/d/yyyy, ToUsDateTimeString => M/d/yyyy h:mm tt, US Eastern, InvariantCulture).
// Delete on merge if the real helpers live in the same namespace.
using System.Globalization;

namespace Reflect.Domain.Helpers;

public static class DateTimeOffsetUsExtensions
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public static DateTimeOffset ToEastern(this DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, Eastern);

    public static string ToUsDateString(this DateTimeOffset value) =>
        value.ToEastern().ToString("M/d/yyyy", CultureInfo.InvariantCulture);

    public static string ToUsDateTimeString(this DateTimeOffset value) =>
        value.ToEastern().ToString("M/d/yyyy h:mm tt", CultureInfo.InvariantCulture);

    public static string ToUsDateString(this DateOnly value) =>
        value.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
}
