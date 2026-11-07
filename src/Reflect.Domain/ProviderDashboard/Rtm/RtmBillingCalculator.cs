namespace Reflect.Domain.ProviderDashboard.Rtm;

public sealed record BillableCode(string Code, string Description, int Units);

public enum RtmBillingStatus
{
    /// <summary>Nothing billable yet this period/month.</summary>
    NotYetBillable = 1,
    /// <summary>At least one code is supported and nothing blocks it.</summary>
    Billable = 2,
    /// <summary>Thresholds are met but something (consent, live interaction) blocks billing.</summary>
    Blocked = 3
}

public sealed class RtmEvaluationInput
{
    public DateOnly Today { get; init; }
    public DateRange DevicePeriod { get; init; }
    public DateRange ManagementMonth { get; init; }
    /// <summary>Distinct calendar days (US Eastern) with any Reflect activity, any range; the calculator filters to the period.</summary>
    public IReadOnlyCollection<DateOnly> DataDays { get; init; } = Array.Empty<DateOnly>();
    public double AttestedMinutes { get; init; }
    public double PendingMinutes { get; init; }
    public bool HasRealTimeInteraction { get; init; }
    public bool HasConsent { get; init; }
    public bool SetupBilled { get; init; }
}

public sealed class RtmEvaluation
{
    public int DataDaysInPeriod { get; init; }
    public int DaysRemainingInPeriod { get; init; }
    public string DeviceCode { get; init; } = string.Empty;
    /// <summary>Data days still needed for the next device tier (0 when the full tier is reached).</summary>
    public int DataDaysToNextTier { get; init; }
    public string NextDeviceCode { get; init; } = string.Empty;
    /// <summary>False when there are not enough days left in the period to reach the next tier.</summary>
    public bool NextTierReachable { get; init; }

    public int ManagementMinutes { get; init; }
    public int PendingMinutes { get; init; }
    public bool HasRealTimeInteraction { get; init; }
    public string NextManagementCode { get; init; } = string.Empty;
    public int MinutesToNextManagementCode { get; init; }

    public List<BillableCode> Codes { get; init; } = new();
    public List<string> Blockers { get; init; } = new();
    public RtmBillingStatus Status { get; init; }

    public bool HasCode(string code) => Codes.Any(c => c.Code == code);
}

/// <summary>A finished 30-day device-supply period and the code its data supports (empty when under 2 days).</summary>
public sealed record CompletedDevicePeriod(DateRange Period, int DataDays, string Code);

/// <summary>Pure function: given monitoring data and attested provider time, which RTM codes are supported?</summary>
public static class RtmBillingCalculator
{
    /// <summary>CBT device-supply code for a number of data days in one 30-day period.</summary>
    public static string DeviceCodeFor(int dataDays) =>
        dataDays >= RtmBillingRules.MinDataDaysFullTier ? RtmBillingRules.CodeCbtDeviceFull
        : dataDays >= RtmBillingRules.MinDataDaysShortTier ? RtmBillingRules.CodeCbtDeviceShort
        : string.Empty;

    public static List<CompletedDevicePeriod> CompletedPeriods(DateOnly rtmStart, IEnumerable<DateOnly> dataDays, DateRange range, DateOnly today)
    {
        var days = dataDays.Distinct().ToList();
        return RtmPeriods.CompletedPeriodsEndingIn(rtmStart, range, today)
            .Select(p =>
            {
                var count = days.Count(p.Contains);
                return new CompletedDevicePeriod(p, count, DeviceCodeFor(count));
            })
            .ToList();
    }

    public static RtmEvaluation Evaluate(RtmEvaluationInput input)
    {
        var codes = new List<BillableCode>();
        var blockers = new List<string>();

        // ---- Device supply (30-day period) ----
        var dataDays = input.DataDays.Distinct().Count(d => input.DevicePeriod.Contains(d) && d <= input.Today);
        var daysRemaining = input.DevicePeriod.DaysRemainingAfter(input.Today);

        string deviceCode = string.Empty, nextDevice;
        int toNextTier;
        if (dataDays >= RtmBillingRules.MinDataDaysFullTier)
        {
            deviceCode = RtmBillingRules.CodeCbtDeviceFull;
            nextDevice = string.Empty;
            toNextTier = 0;
        }
        else if (dataDays >= RtmBillingRules.MinDataDaysShortTier)
        {
            deviceCode = RtmBillingRules.CodeCbtDeviceShort;
            nextDevice = RtmBillingRules.CodeCbtDeviceFull;
            toNextTier = RtmBillingRules.MinDataDaysFullTier - dataDays;
        }
        else
        {
            nextDevice = RtmBillingRules.CodeCbtDeviceShort;
            toNextTier = RtmBillingRules.MinDataDaysShortTier - dataDays;
        }

        if (!string.IsNullOrEmpty(deviceCode))
            codes.Add(new BillableCode(deviceCode, RtmBillingRules.Describe(deviceCode), 1));

        // ---- One-time set-up (requires at least 2 days of monitoring) ----
        if (!input.SetupBilled && dataDays >= RtmBillingRules.MinDataDaysShortTier)
            codes.Add(new BillableCode(RtmBillingRules.CodeSetup, RtmBillingRules.Describe(RtmBillingRules.CodeSetup), 1));

        // ---- Treatment management (calendar month, attested minutes only, no rounding up) ----
        var minutes = (int)Math.Floor(input.AttestedMinutes);
        var managementCodes = new List<BillableCode>();
        string nextMgmt;
        int toNextMgmt;
        if (minutes >= RtmBillingRules.MinMinutesFullManagement)
        {
            managementCodes.Add(new BillableCode(RtmBillingRules.CodeManagementFull, RtmBillingRules.Describe(RtmBillingRules.CodeManagementFull), 1));
            var additional = (minutes - RtmBillingRules.MinMinutesFullManagement) / RtmBillingRules.AdditionalBlockMinutes;
            if (additional > 0)
                managementCodes.Add(new BillableCode(RtmBillingRules.CodeManagementAdditional, RtmBillingRules.Describe(RtmBillingRules.CodeManagementAdditional), additional));
            nextMgmt = RtmBillingRules.CodeManagementAdditional;
            toNextMgmt = RtmBillingRules.MinMinutesFullManagement + (additional + 1) * RtmBillingRules.AdditionalBlockMinutes - minutes;
        }
        else if (minutes >= RtmBillingRules.MinMinutesShortManagement)
        {
            managementCodes.Add(new BillableCode(RtmBillingRules.CodeManagementShort, RtmBillingRules.Describe(RtmBillingRules.CodeManagementShort), 1));
            nextMgmt = RtmBillingRules.CodeManagementFull;
            toNextMgmt = RtmBillingRules.MinMinutesFullManagement - minutes;
        }
        else
        {
            nextMgmt = RtmBillingRules.CodeManagementShort;
            toNextMgmt = RtmBillingRules.MinMinutesShortManagement - minutes;
        }

        var hasBillableWork = codes.Count > 0 || managementCodes.Count > 0;

        if (managementCodes.Count > 0 && !input.HasRealTimeInteraction)
            blockers.Add("Treatment management needs at least one live interaction (video, phone, or in person) this month.");
        else
            codes.AddRange(managementCodes);

        if (!input.HasConsent && hasBillableWork)
            blockers.Add("RTM consent is not on file.");

        var status = !input.HasConsent && hasBillableWork ? RtmBillingStatus.Blocked
            : codes.Count > 0 ? RtmBillingStatus.Billable
            : blockers.Count > 0 ? RtmBillingStatus.Blocked
            : RtmBillingStatus.NotYetBillable;

        if (!input.HasConsent) codes.Clear();

        return new RtmEvaluation
        {
            DataDaysInPeriod = dataDays,
            DaysRemainingInPeriod = daysRemaining,
            DeviceCode = deviceCode,
            NextDeviceCode = nextDevice,
            DataDaysToNextTier = toNextTier,
            NextTierReachable = toNextTier == 0 || toNextTier <= daysRemaining,
            ManagementMinutes = minutes,
            PendingMinutes = (int)Math.Floor(input.PendingMinutes),
            HasRealTimeInteraction = input.HasRealTimeInteraction,
            NextManagementCode = nextMgmt,
            MinutesToNextManagementCode = toNextMgmt,
            Codes = codes,
            Blockers = blockers,
            Status = status
        };
    }
}
