using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Domain.ProviderDashboard.Notifications;

public enum NotificationKind
{
    Urgent = 1,
    Clinical = 2,
    RtmThreshold = 3,
    Attestation = 4
}

/// <summary>A bell notification. <see cref="Key"/> is stable, so "mark as read" survives refreshes until the underlying fact changes.</summary>
public sealed record ProviderNotification(string Key, NotificationKind Kind, Guid ClientId, string Title, string Detail, string Icon);

/// <summary>Turns a client's flags and RTM evaluation into bell notifications. Pure, so it is unit tested.</summary>
public static class ProviderNotificationRules
{
    /// <summary>Remind about the next device tier when the 30-day period ends within this many days.</summary>
    public const int DevicePeriodReminderDays = 5;
    /// <summary>Remind about the next management code when the calendar month ends within this many days.</summary>
    public const int MonthEndReminderDays = 7;
    /// <summary>Only remind when the next management code is this close.</summary>
    public const int MinutesReminderWindow = 10;

    public static List<ProviderNotification> For(Guid clientId, string clientName, IEnumerable<ClinicalFlag> flags, RtmEvaluation rtm,
        DateOnly today, DateOnly periodEnd)
    {
        var list = new List<ProviderNotification>();
        foreach (var f in flags.Where(f => f.Severity is FlagSeverity.Critical or FlagSeverity.Warning))
        {
            var urgent = f.Severity == FlagSeverity.Critical;
            list.Add(new ProviderNotification($"{(urgent ? "urgent" : "flag")}:{clientId:N}:{f.Title}", urgent ? NotificationKind.Urgent : NotificationKind.Clinical,
                clientId, $"{clientName}: {f.Title}", f.Detail, urgent ? "ri-alarm-warning-line" : f.Icon));
        }

        if (rtm == null) return list;

        if (rtm.DataDaysToNextTier > 0 && rtm.NextTierReachable && rtm.DaysRemainingInPeriod <= DevicePeriodReminderDays)
            list.Add(new ProviderNotification($"device:{clientId:N}:{periodEnd:yyyyMMdd}:{rtm.NextDeviceCode}", NotificationKind.RtmThreshold, clientId,
                $"{clientName}: {rtm.DataDaysToNextTier} more data {(rtm.DataDaysToNextTier == 1 ? "day" : "days")} for {rtm.NextDeviceCode}",
                $"Period ends {periodEnd:M/d/yyyy}. A nudge to use Reflect could reach the next code.", "ri-calendar-check-line"));

        var monthEnd = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var monthDaysLeft = monthEnd.DayNumber - today.DayNumber;
        if (!string.IsNullOrEmpty(rtm.NextManagementCode) && rtm.MinutesToNextManagementCode > 0
            && rtm.MinutesToNextManagementCode <= MinutesReminderWindow && monthDaysLeft <= MonthEndReminderDays)
            list.Add(new ProviderNotification($"mgmt:{clientId:N}:{today:yyyyMM}:{rtm.NextManagementCode}", NotificationKind.RtmThreshold, clientId,
                $"{clientName}: {rtm.MinutesToNextManagementCode} min to {rtm.NextManagementCode}",
                $"{monthDaysLeft} {(monthDaysLeft == 1 ? "day" : "days")} left in the month. Only real review time counts.", "ri-time-line"));

        if (rtm.PendingMinutes > 0)
            list.Add(new ProviderNotification($"pending:{clientId:N}:{today:yyyyMMdd}:{rtm.PendingMinutes}", NotificationKind.Attestation, clientId,
                $"{clientName}: {rtm.PendingMinutes} min waiting for attestation",
                "Tracked review time does not count until you attest it.", "ri-timer-line"));
        return list;
    }

    public static IEnumerable<ProviderNotification> Order(IEnumerable<ProviderNotification> all) => all.OrderBy(n => n.Kind).ThenBy(n => n.Title);
}
