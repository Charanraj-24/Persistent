using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Notifications;
using Reflect.Domain.ProviderDashboard.Rtm;
using Xunit;

namespace Reflect.Domain.Tests;

public class ProviderNotificationRulesTests
{
    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Critical_flag_becomes_urgent_and_info_flags_are_ignored()
    {
        var flags = new[]
        {
            new ClinicalFlag(FlagSeverity.Critical, "PHQ-9 item 9 endorsed", "d", "i"),
            new ClinicalFlag(FlagSeverity.Info, "New client", "d", "i"),
            new ClinicalFlag(FlagSeverity.Positive, "Mood lifting", "d", "i")
        };
        var n = ProviderNotificationRules.For(Id, "A B", flags, null, new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 20));
        Assert.Single(n);
        Assert.Equal(NotificationKind.Urgent, n[0].Kind);
    }

    [Fact]
    public void Device_tier_reminder_only_near_period_end_and_when_reachable()
    {
        var near = new RtmEvaluation { DataDaysToNextTier = 2, NextTierReachable = true, DaysRemainingInPeriod = 3, NextDeviceCode = "98978" };
        var far = new RtmEvaluation { DataDaysToNextTier = 2, NextTierReachable = true, DaysRemainingInPeriod = 12, NextDeviceCode = "98978" };
        var unreachable = new RtmEvaluation { DataDaysToNextTier = 9, NextTierReachable = false, DaysRemainingInPeriod = 3, NextDeviceCode = "98978" };
        var today = new DateOnly(2026, 11, 4);
        Assert.Contains(ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), near, today, today.AddDays(3)), x => x.Kind == NotificationKind.RtmThreshold);
        Assert.Empty(ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), far, today, today.AddDays(12)));
        Assert.Empty(ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), unreachable, today, today.AddDays(3)));
    }

    [Fact]
    public void Management_reminder_only_in_last_week_of_month()
    {
        var rtm = new RtmEvaluation { NextManagementCode = "98980", MinutesToNextManagementCode = 6 };
        Assert.Empty(ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), rtm, new DateOnly(2026, 11, 4), new DateOnly(2026, 12, 1)));
        var late = ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), rtm, new DateOnly(2026, 11, 26), new DateOnly(2026, 12, 1));
        Assert.Contains(late, x => x.Title.Contains("6 min to 98980"));
    }

    [Fact]
    public void Pending_minutes_raise_attestation_reminder_with_stable_key()
    {
        var rtm = new RtmEvaluation { PendingMinutes = 4 };
        var a = ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), rtm, new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 20));
        var b = ProviderNotificationRules.For(Id, "A B", Array.Empty<ClinicalFlag>(), rtm, new DateOnly(2026, 11, 4), new DateOnly(2026, 11, 20));
        Assert.Equal(NotificationKind.Attestation, Assert.Single(a).Kind);
        Assert.Equal(a[0].Key, b[0].Key);
    }
}
