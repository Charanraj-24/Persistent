using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;

namespace Reflect.Domain.Tests;

public class OutcomesCalculatorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private static AssessmentResult[] Phq(params int[] scores) =>
        scores.Select((s, i) => new AssessmentResult { Instrument = AssessmentInstrument.Phq9, TotalScore = s, CompletedAt = T0.AddDays(14 * i) }).ToArray();

    [Fact]
    public void Response_and_remission_follow_conventions()
    {
        var o = OutcomesCalculator.ForClient(Phq(16, 12, 4), AssessmentInstrument.Phq9);
        Assert.True(o.IsResponse);
        Assert.True(o.IsRemission);
        Assert.True(o.Improved);
        Assert.Equal(-12, o.Change);
    }

    [Fact]
    public void Low_baseline_never_counts_as_response()
    {
        var o = OutcomesCalculator.ForClient(Phq(8, 3), AssessmentInstrument.Phq9);
        Assert.False(o.ClinicallyElevatedAtBaseline);
        Assert.False(o.IsResponse);
        Assert.True(o.Improved);
    }

    [Fact]
    public void Summary_counts_across_clients_and_skips_single_measurements()
    {
        var s = OutcomesCalculator.Summarize(new[] { Phq(18, 8), Phq(12, 17), Phq(10, 9), Phq(15) }, AssessmentInstrument.Phq9);
        Assert.Equal(3, s.Measured);
        Assert.Equal(3, s.ElevatedAtBaseline);
        Assert.Equal(1, s.Responders);
        Assert.Equal(1, s.Improved);
        Assert.Equal(1, s.Worsened);
        Assert.Equal(0, s.Remitted);
    }
}
