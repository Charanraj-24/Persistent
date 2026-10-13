using System.Globalization;
using Reflect.Domain.Common;

namespace Reflect.Domain.ProviderDashboard.Rules;

/// <summary>An RTM billing period: one US Eastern calendar month. Start and End are inclusive calendar days.</summary>
public sealed record BillingPeriod(DateOnly Start, DateOnly End)
{
    public static BillingPeriod ForMonth(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return new BillingPeriod(start, start.AddMonths(1).AddDays(-1));
    }

    public static BillingPeriod Containing(DateOnly day) => ForMonth(day.Year, day.Month);

    public DateTimeOffset StartUtc => Start.StartOfEasternDayUtc();

    /// <summary>Exclusive upper bound: start of the day after <see cref="End"/>.</summary>
    public DateTimeOffset EndUtcExclusive => End.AddDays(1).StartOfEasternDayUtc();

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    public string Key => Start.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    public string Label => Start.ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    public bool Contains(DateOnly day) => day >= Start && day <= End;

    public BillingPeriod Previous() => ForMonth(Start.AddMonths(-1).Year, Start.AddMonths(-1).Month);

    public BillingPeriod Next() => ForMonth(Start.AddMonths(1).Year, Start.AddMonths(1).Month);

    public static bool TryParseKey(string key, out BillingPeriod period)
    {
        period = null;
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (!DateOnly.TryParseExact(key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day)) return false;
        period = ForMonth(day.Year, day.Month);
        return true;
    }
}
