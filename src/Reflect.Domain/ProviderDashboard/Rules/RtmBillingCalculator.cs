namespace Reflect.Domain.ProviderDashboard.Rules;

public enum RtmReadiness
{
    NotEnrolled = 0,
    NotBillable = 1,
    AtRisk = 2,
    OnTrack = 3,
    Partial = 4,
    Ready = 5,
    /// <summary>16 data days are still possible, but only with a session on most remaining days.</summary>
    BehindPace = 6
}

public sealed record BillableCode(string Code, int Units, string Description);

public sealed class RtmPeriodInput
{
    public BillingPeriod Period { get; init; }
    public DateOnly Today { get; init; }
    /// <summary>Eastern calendar days with at least one remote (not in-clinic) session.</summary>
    public IReadOnlyCollection<DateOnly> RemoteDataDays { get; init; } = Array.Empty<DateOnly>();
    public int MonitoringSeconds { get; init; }
    public int InteractiveCommunicationCount { get; init; }
    public int InteractiveCommunicationMinutes { get; init; }
    public DateOnly? RtmConsentOn { get; init; }
}

public sealed class RtmBillingSummary
{
    public RtmReadiness Readiness { get; init; }
    public int DataDays { get; init; }
    public int DataDaysRequired => RtmBillingRules.FullDeviceSupplyDays;
    public bool CanStillReachFullDataDays { get; init; }
    public int DaysLeftInPeriod { get; init; }
    public int MonitoringMinutes { get; init; }
    public int InteractiveMinutes { get; init; }
    public int TreatmentMinutes { get; init; }
    public bool HasInteractiveCommunication { get; init; }
    public int? NextThresholdMinutes { get; init; }
    public int MinutesToNextThreshold { get; init; }
    public IReadOnlyList<BillableCode> Codes { get; init; } = Array.Empty<BillableCode>();
    public IReadOnlyList<string> Blockers { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Reference RTM rules (CPT 98975-98986, cognitive behavioral therapy monitoring). Kept in one place so a billing
/// or compliance team can adjust them. This is decision support for the provider, not a claim submission.
/// </summary>
public static class RtmBillingRules
{
    public const int FullDeviceSupplyDays = 16;
    public const int ShortDeviceSupplyMinDays = 2;
    public const int ShortTreatmentMinutes = 10;
    public const int FirstTreatmentBlockMinutes = 20;
    public const int AdditionalTreatmentBlockMinutes = 20;
    /// <summary>Behind pace when the remaining data days needed exceed this share of the open days left.</summary>
    public const double BehindPaceShare = 0.75;

    public const string InitialSetup = "98975";
    public const string DeviceSupplyCbt = "98978";
    public const string DeviceSupplyCbtShort = "98986";
    public const string TreatmentShort = "98979";
    public const string TreatmentFirst20 = "98980";
    public const string TreatmentEachAdditional20 = "98981";

    public static string Describe(string code) => code switch
    {
        InitialSetup => "Initial set-up and patient education",
        DeviceSupplyCbt => "Device supply, CBT monitoring (16+ days of data)",
        DeviceSupplyCbtShort => "Device supply, CBT monitoring (2-15 days of data)",
        TreatmentShort => "Treatment management, 10-19 minutes",
        TreatmentFirst20 => "Treatment management, first 20 minutes",
        TreatmentEachAdditional20 => "Treatment management, each additional 20 minutes",
        _ => code
    };
}

public static class RtmBillingCalculator
{
    public static RtmBillingSummary Calculate(RtmPeriodInput input)
    {
        var period = input.Period;
        var dataDays = input.RemoteDataDays.Where(period.Contains).Distinct().ToHashSet();
        var monitoringMinutes = input.MonitoringSeconds / 60;
        var interactiveMinutes = Math.Max(0, input.InteractiveCommunicationMinutes);
        var treatmentMinutes = monitoringMinutes + interactiveMinutes;
        var hasInteractive = input.InteractiveCommunicationCount > 0;

        var periodClosed = input.Today > period.End;
        var firstOpenDay = input.Today < period.Start ? period.Start : input.Today;
        var daysLeft = periodClosed ? 0 : period.End.DayNumber - firstOpenDay.DayNumber + 1;
        var openDaysWithoutData = 0;
        for (var d = firstOpenDay; !periodClosed && d <= period.End; d = d.AddDays(1))
        {
            if (!dataDays.Contains(d)) openDaysWithoutData++;
        }
        var canReachFull = dataDays.Count + openDaysWithoutData >= RtmBillingRules.FullDeviceSupplyDays;

        var (nextThreshold, minutesToNext) = NextThreshold(treatmentMinutes);

        var consented = input.RtmConsentOn.HasValue && input.RtmConsentOn.Value <= period.End;
        if (!consented)
        {
            return new RtmBillingSummary
            {
                Readiness = RtmReadiness.NotEnrolled,
                DataDays = dataDays.Count,
                CanStillReachFullDataDays = canReachFull,
                DaysLeftInPeriod = daysLeft,
                MonitoringMinutes = monitoringMinutes,
                InteractiveMinutes = interactiveMinutes,
                TreatmentMinutes = treatmentMinutes,
                HasInteractiveCommunication = hasInteractive,
                NextThresholdMinutes = nextThreshold,
                MinutesToNextThreshold = minutesToNext,
                Blockers = new[] { "No RTM consent on file for this period." }
            };
        }

        var codes = new List<BillableCode>();
        var blockers = new List<string>();

        if (period.Contains(input.RtmConsentOn.Value) && dataDays.Count >= RtmBillingRules.ShortDeviceSupplyMinDays)
        {
            codes.Add(Code(RtmBillingRules.InitialSetup, 1));
        }

        if (dataDays.Count >= RtmBillingRules.FullDeviceSupplyDays)
        {
            codes.Add(Code(RtmBillingRules.DeviceSupplyCbt, 1));
        }
        else if (dataDays.Count >= RtmBillingRules.ShortDeviceSupplyMinDays)
        {
            codes.Add(Code(RtmBillingRules.DeviceSupplyCbtShort, 1));
            blockers.Add($"{dataDays.Count} of {RtmBillingRules.FullDeviceSupplyDays} remote data days for full device supply.");
        }
        else
        {
            blockers.Add($"Only {dataDays.Count} remote data day(s); at least {RtmBillingRules.ShortDeviceSupplyMinDays} are needed.");
        }

        if (treatmentMinutes >= RtmBillingRules.ShortTreatmentMinutes && !hasInteractive)
        {
            blockers.Add("No interactive (video or phone) communication logged this period.");
        }
        else if (treatmentMinutes >= RtmBillingRules.FirstTreatmentBlockMinutes)
        {
            codes.Add(Code(RtmBillingRules.TreatmentFirst20, 1));
            var additional = (treatmentMinutes - RtmBillingRules.FirstTreatmentBlockMinutes) / RtmBillingRules.AdditionalTreatmentBlockMinutes;
            if (additional > 0) codes.Add(Code(RtmBillingRules.TreatmentEachAdditional20, additional));
        }
        else if (treatmentMinutes >= RtmBillingRules.ShortTreatmentMinutes)
        {
            codes.Add(Code(RtmBillingRules.TreatmentShort, 1));
        }
        else
        {
            blockers.Add($"{treatmentMinutes} of {RtmBillingRules.ShortTreatmentMinutes} treatment-management minutes.");
        }

        var full = dataDays.Count >= RtmBillingRules.FullDeviceSupplyDays && codes.Any(c => c.Code == RtmBillingRules.TreatmentFirst20);
        RtmReadiness readiness;
        if (full) readiness = RtmReadiness.Ready;
        else if (periodClosed) readiness = codes.Count > 0 ? RtmReadiness.Partial : RtmReadiness.NotBillable;
        else if (!canReachFull) readiness = RtmReadiness.AtRisk;
        else if (IsBehindPace(dataDays.Count, openDaysWithoutData)) readiness = RtmReadiness.BehindPace;
        else readiness = RtmReadiness.OnTrack;

        return new RtmBillingSummary
        {
            Readiness = readiness,
            DataDays = dataDays.Count,
            CanStillReachFullDataDays = canReachFull,
            DaysLeftInPeriod = daysLeft,
            MonitoringMinutes = monitoringMinutes,
            InteractiveMinutes = interactiveMinutes,
            TreatmentMinutes = treatmentMinutes,
            HasInteractiveCommunication = hasInteractive,
            NextThresholdMinutes = nextThreshold,
            MinutesToNextThreshold = minutesToNext,
            Codes = codes,
            Blockers = blockers
        };
    }

    public static bool IsBehindPace(int dataDays, int openDaysWithoutData)
    {
        var stillNeeded = RtmBillingRules.FullDeviceSupplyDays - dataDays;
        return stillNeeded > 0 && stillNeeded > openDaysWithoutData * RtmBillingRules.BehindPaceShare;
    }

    /// <summary>The next treatment-time threshold (10, 20, 40, 60, ...) and how many minutes remain to reach it.</summary>
    public static (int threshold, int minutesToGo) NextThreshold(int treatmentMinutes)
    {
        int next;
        if (treatmentMinutes < RtmBillingRules.ShortTreatmentMinutes) next = RtmBillingRules.ShortTreatmentMinutes;
        else if (treatmentMinutes < RtmBillingRules.FirstTreatmentBlockMinutes) next = RtmBillingRules.FirstTreatmentBlockMinutes;
        else
        {
            var blocks = (treatmentMinutes - RtmBillingRules.FirstTreatmentBlockMinutes) / RtmBillingRules.AdditionalTreatmentBlockMinutes + 1;
            next = RtmBillingRules.FirstTreatmentBlockMinutes + blocks * RtmBillingRules.AdditionalTreatmentBlockMinutes;
        }
        return (next, next - treatmentMinutes);
    }

    private static BillableCode Code(string code, int units) => new(code, units, RtmBillingRules.Describe(code));
}
