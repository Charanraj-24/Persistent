using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

public sealed record PeriodOption(string Key, string Label, bool Selected);

public static class PeriodOptions
{
    public const int MonthsBack = 6;

    /// <summary>Parses a "yyyy-MM" key. Falls back to the current month; future and very old months are not allowed.</summary>
    public static BillingPeriod Resolve(string key, DateOnly today)
    {
        var current = BillingPeriod.Containing(today);
        if (!BillingPeriod.TryParseKey(key, out var period)) return current;
        if (period.Start > current.Start) return current;
        if (period.Start < current.Start.AddMonths(-(MonthsBack - 1))) return current;
        return period;
    }

    public static IReadOnlyList<PeriodOption> List(BillingPeriod selected, DateOnly today)
    {
        var options = new List<PeriodOption>();
        var period = BillingPeriod.Containing(today);
        for (var i = 0; i < MonthsBack; i++)
        {
            var label = i == 0 ? $"{period.Label} (current)" : period.Label;
            options.Add(new PeriodOption(period.Key, label, period.Key == selected.Key));
            period = period.Previous();
        }
        return options;
    }
}
