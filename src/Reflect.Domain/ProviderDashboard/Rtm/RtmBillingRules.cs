namespace Reflect.Domain.ProviderDashboard.Rtm;

/// <summary>
/// CPT Remote Therapeutic Monitoring rules as of the CY2026 Medicare Physician Fee Schedule.
/// Reflect is a cognitive-behavioral-therapy-type monitoring tool, so the CBT device-supply codes apply.
/// Keep every threshold here so a rule change is a one-file edit.
/// </summary>
public static class RtmBillingRules
{
    public const int DevicePeriodDays = 30;

    // Device supply (patient data transmission) per 30-day period. The two tiers are mutually exclusive.
    public const int MinDataDaysShortTier = 2;   // 98986: 2-15 days
    public const int MinDataDaysFullTier = 16;   // 98978: 16-30 days

    // Treatment management (provider time) per calendar month. No "halfway" rounding.
    public const int MinMinutesShortManagement = 10;   // 98979: first 10 minutes (10-19)
    public const int MinMinutesFullManagement = 20;    // 98980: first 20 minutes
    public const int AdditionalBlockMinutes = 20;      // 98981: each additional 20 minutes

    public const string CodeSetup = "98975";
    public const string CodeCbtDeviceShort = "98986";
    public const string CodeCbtDeviceFull = "98978";
    public const string CodeManagementShort = "98979";
    public const string CodeManagementFull = "98980";
    public const string CodeManagementAdditional = "98981";

    public static string Describe(string code) => code switch
    {
        CodeSetup => "Initial set-up and patient education",
        CodeCbtDeviceShort => "CBT monitoring device supply, 2-15 days",
        CodeCbtDeviceFull => "CBT monitoring device supply, 16-30 days",
        CodeManagementShort => "Treatment management, first 10 minutes",
        CodeManagementFull => "Treatment management, first 20 minutes",
        CodeManagementAdditional => "Treatment management, each additional 20 minutes",
        _ => code
    };
}
