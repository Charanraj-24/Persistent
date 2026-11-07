using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Domain.ProviderDashboard.Clinical;

public sealed record ClientOutcome(int Baseline, int Latest, bool ClinicallyElevatedAtBaseline)
{
    public int Change => Latest - Baseline;
    /// <summary>Response: at least 50% reduction from an elevated baseline (measurement-based care convention).</summary>
    public bool IsResponse => ClinicallyElevatedAtBaseline && Latest <= Baseline / 2.0;
    /// <summary>Remission: score below 5 after an elevated baseline.</summary>
    public bool IsRemission => ClinicallyElevatedAtBaseline && Latest < 5;
    public bool Improved => Change <= -AssessmentScoring.MeaningfulChange;
    public bool Worsened => Change >= AssessmentScoring.MeaningfulChange;
}

public sealed class OutcomeSummary
{
    public AssessmentInstrument Instrument { get; init; }
    public int Measured { get; init; }
    public int ElevatedAtBaseline { get; init; }
    public int Improved { get; init; }
    public int Worsened { get; init; }
    public int Responders { get; init; }
    public int Remitted { get; init; }
    public double AverageBaseline { get; init; }
    public double AverageLatest { get; init; }
    public IReadOnlyList<ClientOutcome> Outcomes { get; init; } = Array.Empty<ClientOutcome>();

    public double Pct(int n, int of) => of == 0 ? 0 : 100.0 * n / of;
}

/// <summary>Caseload outcomes from repeated PHQ-9 / GAD-7 (baseline = first in window, latest = most recent).</summary>
public static class OutcomesCalculator
{
    /// <summary>PHQ-9 and GAD-7 scores of 10+ are the usual "moderate or above" threshold.</summary>
    public const int ElevatedThreshold = 10;

    public static ClientOutcome ForClient(IEnumerable<AssessmentResult> assessments, AssessmentInstrument instrument)
    {
        var series = assessments.Where(a => !a.IsDeleted && a.Instrument == instrument).OrderBy(a => a.CompletedAt).ToList();
        if (series.Count < 2) return null;
        return new ClientOutcome(series[0].TotalScore, series[^1].TotalScore, series[0].TotalScore >= ElevatedThreshold);
    }

    public static OutcomeSummary Summarize(IEnumerable<IEnumerable<AssessmentResult>> perClient, AssessmentInstrument instrument)
    {
        var outcomes = perClient.Select(a => ForClient(a, instrument)).Where(o => o != null).ToList();
        var elevated = outcomes.Where(o => o.ClinicallyElevatedAtBaseline).ToList();
        return new OutcomeSummary
        {
            Instrument = instrument,
            Measured = outcomes.Count,
            ElevatedAtBaseline = elevated.Count,
            Improved = outcomes.Count(o => o.Improved),
            Worsened = outcomes.Count(o => o.Worsened),
            Responders = elevated.Count(o => o.IsResponse),
            Remitted = elevated.Count(o => o.IsRemission),
            AverageBaseline = outcomes.Count == 0 ? 0 : outcomes.Average(o => o.Baseline),
            AverageLatest = outcomes.Count == 0 ? 0 : outcomes.Average(o => o.Latest),
            Outcomes = outcomes
        };
    }
}
