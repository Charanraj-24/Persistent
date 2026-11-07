using System.Globalization;

namespace Reflect.Domain.ProviderDashboard.Rtm;

public sealed record ReimbursementEstimate(decimal Amount, int UnpricedUnits)
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");
    public string Formatted => Amount.ToString("C2", Us);
    /// <summary>Rounded for headline use, for example "$1,040".</summary>
    public string Rounded => Math.Round(Amount, 0).ToString("C0", Us);
}

/// <summary>
/// CY2026 Medicare Physician Fee Schedule national average payments (non-facility, non-APM) for the RTM codes the
/// dashboard supports. Actual payment varies by locality and payer. The CBT device-supply codes 98978 and 98986 have
/// no national rate (Medicare Administrative Contractor priced), so they're counted but not priced.
/// </summary>
public static class RtmFeeSchedule
{
    public const string Source = "CMS CY2026 Physician Fee Schedule, national average, non-facility";

    private static readonly IReadOnlyDictionary<string, decimal> Rates = new Dictionary<string, decimal>
    {
        [RtmBillingRules.CodeSetup] = 21.71m,
        [RtmBillingRules.CodeManagementShort] = 26.39m,
        [RtmBillingRules.CodeManagementFull] = 54.11m,
        [RtmBillingRules.CodeManagementAdditional] = 41.42m
    };

    public static decimal? RateFor(string code) => Rates.TryGetValue(code, out var r) ? r : null;

    public static ReimbursementEstimate Estimate(IEnumerable<BillableCode> codes)
    {
        decimal total = 0;
        var unpriced = 0;
        foreach (var c in codes)
        {
            var rate = RateFor(c.Code);
            if (rate.HasValue) total += rate.Value * c.Units; else unpriced += c.Units;
        }
        return new ReimbursementEstimate(total, unpriced);
    }

    public static ReimbursementEstimate Estimate(IReadOnlyDictionary<string, int> unitsByCode) =>
        Estimate(unitsByCode.Select(kv => new BillableCode(kv.Key, string.Empty, kv.Value)));
}
