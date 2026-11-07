using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.Tests;

public class ProviderRtmRollupTests
{
    private static RtmEvaluation Eval(int minutes, bool live, int pending = 0) => RtmBillingCalculator.Evaluate(new RtmEvaluationInput
    {
        Today = new DateOnly(2026, 10, 30),
        DevicePeriod = new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 30)),
        AttestedMinutes = minutes, PendingMinutes = pending, HasRealTimeInteraction = live, HasConsent = true, SetupBilled = true
    });

    [Fact]
    public void Rollup_counts_clients_minutes_codes_and_gaps()
    {
        var r = ProviderRtmRollup.From(new[]
        {
            new ClientMonthResult(Eval(25, live: true), new[] { new BillableCode("98980", "", 1), new BillableCode("98978", "", 1) }, true),
            new ClientMonthResult(Eval(45, live: true, pending: 4), new[] { new BillableCode("98980", "", 1), new BillableCode("98981", "", 1) }, true),
            new ClientMonthResult(Eval(15, live: false), Array.Empty<BillableCode>(), true),
            new ClientMonthResult(Eval(0, live: false), Array.Empty<BillableCode>(), false)
        });

        Assert.Equal(4, r.ClientsOnRtm);
        Assert.Equal(2, r.ClientsWithCodes);
        Assert.Equal(85, r.AttestedMinutes);
        Assert.Equal(4, r.PendingMinutes);
        Assert.Equal(2, r.ClientsWithLiveInteraction);
        Assert.Equal(1, r.ClientsMissingLiveInteraction);
        Assert.Equal(1, r.ClientsMissingConsent);
        Assert.Equal(1, r.ClientsWithPendingTime);
        Assert.Equal(3, r.DocumentationGaps);
        Assert.Equal(2, r.CodeUnits["98980"]);
        Assert.Equal(1, r.CodeUnits["98981"]);
    }

    [Fact]
    public void Empty_caseload_rolls_up_to_zero()
    {
        var r = ProviderRtmRollup.From(Array.Empty<ClientMonthResult>());
        Assert.Equal(0, r.ClientsOnRtm);
        Assert.Empty(r.CodeUnits);
    }
}
