using System.Globalization;
using Reflect.Domain.ProviderDashboard.Rtm;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

/// <summary>Turns an RTM evaluation into short, provider-friendly UI text.</summary>
public static class RtmStatusPresenter
{
    public static BadgeViewModel StatusBadge(RtmEvaluation e) => e.Status switch
    {
        RtmBillingStatus.Billable when e.Blockers.Count == 0 => new BadgeViewModel("success", "Billable", "ri-checkbox-circle-line"),
        RtmBillingStatus.Billable => new BadgeViewModel("warning", "Partly billable", "ri-error-warning-line"),
        RtmBillingStatus.Blocked => new BadgeViewModel("danger", "Action needed", "ri-close-circle-line"),
        _ => new BadgeViewModel("info", "In progress", "ri-loader-2-line")
    };

    public static string CodesText(RtmEvaluation e) =>
        e.Codes.Count == 0 ? "None yet" : string.Join(", ", e.Codes.Select(c => c.Units > 1 ? $"{c.Code} x{c.Units}" : c.Code));

    /// <summary>The single most useful next step, or empty when nothing is left to do this month.</summary>
    public static string NextStep(RtmEvaluation e)
    {
        if (e.Blockers.Count > 0) return e.Blockers[0];
        if (e.PendingMinutes > 0) return string.Create(CultureInfo.InvariantCulture, $"Attest {e.PendingMinutes} pending min.");
        if (e.MinutesToNextManagementCode > 0 && e.MinutesToNextManagementCode <= 10)
            return string.Create(CultureInfo.InvariantCulture, $"{e.MinutesToNextManagementCode} min to {e.NextManagementCode}.");
        if (e.DataDaysToNextTier > 0)
        {
            return e.NextTierReachable
                ? string.Create(CultureInfo.InvariantCulture, $"{e.DataDaysToNextTier} more data {(e.DataDaysToNextTier == 1 ? "day" : "days")} for {e.NextDeviceCode} ({e.DaysRemainingInPeriod} left).")
                : string.Create(CultureInfo.InvariantCulture, $"{e.NextDeviceCode} out of reach this period.");
        }
        return string.Empty;
    }

    public static int DataDaysPercent(RtmEvaluation e) =>
        (int)Math.Round(Math.Min(1.0, e.DataDaysInPeriod / (double)RtmBillingRules.MinDataDaysFullTier) * 100);

    public static int MinutesPercent(RtmEvaluation e) =>
        (int)Math.Round(Math.Min(1.0, e.ManagementMinutes / (double)RtmBillingRules.MinMinutesFullManagement) * 100);
}
