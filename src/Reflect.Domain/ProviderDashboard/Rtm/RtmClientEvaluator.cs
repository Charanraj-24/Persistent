using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Domain.ProviderDashboard.Rtm;

/// <summary>Builds the calculator input from entities. Shared by the caseload, client detail and report screens.</summary>
public static class RtmClientEvaluator
{
    public static RtmEvaluation Evaluate(
        ProviderClient client,
        IEnumerable<AppSession> sessions,
        IEnumerable<RtmTimeEntry> timeEntries,
        DateOnly today,
        Func<DateTimeOffset, DateOnly> toLocalDate)
    {
        var devicePeriod = RtmPeriods.DevicePeriodFor(client.RtmStartDate, today);
        var month = RtmPeriods.CalendarMonthOf(today);

        var monthEntries = timeEntries
            .Where(e => !e.IsDeleted && e.ClientId == client.Id && month.Contains(toLocalDate(e.StartedAt)))
            .ToList();

        return RtmBillingCalculator.Evaluate(new RtmEvaluationInput
        {
            Today = today,
            DevicePeriod = devicePeriod,
            ManagementMonth = month,
            DataDays = sessions.Where(s => !s.IsDeleted && s.ClientUserId == client.ClientUserId)
                .Select(s => toLocalDate(s.StartedAt)).Distinct().ToList(),
            AttestedMinutes = monthEntries.Where(e => e.IsAttested).Sum(e => e.Minutes),
            PendingMinutes = monthEntries.Where(e => !e.IsAttested).Sum(e => e.Minutes),
            HasRealTimeInteraction = monthEntries.Any(e => e.IsAttested && e.IsRealTimeInteraction),
            HasConsent = client.RtmConsentDate.HasValue,
            SetupBilled = client.SetupBilled
        });
    }

    /// <summary>
    /// Codes ready to bill for a month: device-supply codes from periods that ended in the month,
    /// plus set-up and treatment-management codes from the month's evaluation. Nothing without consent.
    /// </summary>
    public static List<BillableCode> CodesForMonth(RtmEvaluation monthEvaluation, IEnumerable<CompletedDevicePeriod> completedInMonth, bool hasConsent)
    {
        if (!hasConsent) return new List<BillableCode>();
        var managementCodes = new HashSet<string>
        {
            RtmBillingRules.CodeSetup, RtmBillingRules.CodeManagementShort,
            RtmBillingRules.CodeManagementFull, RtmBillingRules.CodeManagementAdditional
        };
        return completedInMonth.Where(p => !string.IsNullOrEmpty(p.Code))
            .Select(p => new BillableCode(p.Code, RtmBillingRules.Describe(p.Code), 1))
            .Concat(monthEvaluation.Codes.Where(c => managementCodes.Contains(c.Code)))
            .ToList();
    }

    /// <summary>Completed device-supply periods ending in <paramref name="range"/> (billable once finished).</summary>
    public static List<CompletedDevicePeriod> CompletedPeriods(
        ProviderClient client, IEnumerable<AppSession> sessions, DateRange range, DateOnly today, Func<DateTimeOffset, DateOnly> toLocalDate) =>
        RtmBillingCalculator.CompletedPeriods(
            client.RtmStartDate,
            sessions.Where(s => !s.IsDeleted && s.ClientUserId == client.ClientUserId).Select(s => toLocalDate(s.StartedAt)),
            range, today);
}

/// <summary>Guards for the dashboard's active-time tracker so time cannot be inflated by the browser.</summary>
public static class ReviewTimeRules
{
    /// <summary>The browser sends a heartbeat every 15 seconds of active viewing.</summary>
    public const int HeartbeatIntervalSeconds = 15;

    /// <summary>Largest increment accepted from one heartbeat (interval plus network slack).</summary>
    public const int MaxSecondsPerHeartbeat = 20;

    /// <summary>
    /// Seconds to add for one heartbeat. The claimed value is clamped to the allowed maximum and
    /// to the wall-clock time since the previous heartbeat, so replaying or racing requests adds nothing.
    /// </summary>
    public static int AcceptedIncrement(int claimedSeconds, DateTimeOffset? lastHeartbeatUtc, DateTimeOffset nowUtc)
    {
        if (claimedSeconds <= 0) return 0;
        var accepted = Math.Min(claimedSeconds, MaxSecondsPerHeartbeat);
        if (lastHeartbeatUtc.HasValue)
        {
            var elapsed = (int)Math.Floor((nowUtc - lastHeartbeatUtc.Value).TotalSeconds) + 2; // 2 s clock slack
            accepted = Math.Min(accepted, Math.Max(0, elapsed));
        }
        return accepted;
    }
}
