using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.Tests;

public class RtmFeeScheduleTests
{
    [Fact]
    public void Management_codes_are_priced_and_units_multiply()
    {
        var e = RtmFeeSchedule.Estimate(new[] { new BillableCode("98980", "", 1), new BillableCode("98981", "", 2) });
        Assert.Equal(54.11m + 2 * 41.42m, e.Amount);
        Assert.Equal(0, e.UnpricedUnits);
        Assert.Equal("$136.95", e.Formatted);
    }

    [Fact]
    public void Contractor_priced_device_codes_are_counted_not_priced()
    {
        var e = RtmFeeSchedule.Estimate(new[] { new BillableCode("98978", "", 1), new BillableCode("98986", "", 1), new BillableCode("98975", "", 1) });
        Assert.Equal(21.71m, e.Amount);
        Assert.Equal(2, e.UnpricedUnits);
    }

    [Fact]
    public void Dictionary_overload_matches()
    {
        var e = RtmFeeSchedule.Estimate(new Dictionary<string, int> { ["98979"] = 3 });
        Assert.Equal(79.17m, e.Amount);
        Assert.Equal("$79", e.Rounded);
    }
}
