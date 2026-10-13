using Reflect.Domain.Common;

namespace Reflect.Domain.Tests;

public class UsDateTimeExtensionsTests
{
    [Fact]
    public void Formats_in_us_eastern_with_12_hour_clock()
    {
        var utc = new DateTimeOffset(2026, 10, 13, 3, 5, 0, TimeSpan.Zero); // 11:05 PM EDT on Oct 12

        Assert.Equal("10/12/2026", utc.ToUsDateString());
        Assert.Equal("10/12/2026 11:05 PM", utc.ToUsDateTimeString());
        Assert.Equal(new DateOnly(2026, 10, 12), utc.ToEasternDate());
    }
}
