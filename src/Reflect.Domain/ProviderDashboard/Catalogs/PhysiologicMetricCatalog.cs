namespace Reflect.Domain.ProviderDashboard.Catalogs;

public sealed record PhysiologicMetric(string Code, string Name, string Unit, string Icon, decimal TypicalLow, decimal TypicalHigh);

/// <summary>Metrics the RPM preview can show. Add a row here to support a new measure; no schema change needed.</summary>
public static class PhysiologicMetricCatalog
{
    public const string HeartRate = "heart-rate";
    public const string HeartRateVariability = "hrv-rmssd";

    public static readonly IReadOnlyList<PhysiologicMetric> All = new List<PhysiologicMetric>
    {
        new(HeartRate, "Heart rate", "bpm", "ri-heart-pulse-line", 60, 100),
        new(HeartRateVariability, "Heart rate variability (RMSSD)", "ms", "ri-pulse-line", 20, 80)
    };

    public static PhysiologicMetric Get(string code) =>
        All.FirstOrDefault(m => m.Code == code) ?? new PhysiologicMetric(code, code, string.Empty, "ri-pulse-line", 0, 0);
}
