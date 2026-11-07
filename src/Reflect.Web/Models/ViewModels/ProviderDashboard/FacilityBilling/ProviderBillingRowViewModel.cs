using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.FacilityBilling;

/// <summary>One provider's month in the facility billing view. Counts only, no client identity or clinical data.</summary>
public class ProviderBillingRowViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Credentials { get; set; } = string.Empty;
    public string Npi { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public int ClientsOnRtm { get; set; }
    public int ClientsWithCodes { get; set; }
    public int AttestedMinutes { get; set; }
    public int PendingMinutes { get; set; }
    public int ClientsWithLiveInteraction { get; set; }
    public int ClientsMissingLiveInteraction { get; set; }
    public int ClientsMissingConsent { get; set; }
    public int ClientsWithPendingTime { get; set; }
    public int DocumentationGaps { get; set; }
    public string Codes { get; set; } = string.Empty;
    public IReadOnlyDictionary<string, int> CodeUnits { get; set; } = new Dictionary<string, int>();
    public BadgeViewModel Status { get; set; }
    public ReimbursementEstimate Estimate => RtmFeeSchedule.Estimate(CodeUnits);

    public static ProviderBillingRowViewModel FromDomain(ProviderProfile p, ProviderRtmRollup r) => new()
    {
        Name = p.FullName,
        Credentials = p.Credentials,
        Npi = p.Npi,
        Initials = string.Concat(p.FullName.Replace("Dr. ", string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(s => s[0])),
        ClientsOnRtm = r.ClientsOnRtm,
        ClientsWithCodes = r.ClientsWithCodes,
        AttestedMinutes = r.AttestedMinutes,
        PendingMinutes = r.PendingMinutes,
        ClientsWithLiveInteraction = r.ClientsWithLiveInteraction,
        ClientsMissingLiveInteraction = r.ClientsMissingLiveInteraction,
        ClientsMissingConsent = r.ClientsMissingConsent,
        ClientsWithPendingTime = r.ClientsWithPendingTime,
        DocumentationGaps = r.DocumentationGaps,
        CodeUnits = r.CodeUnits,
        Codes = r.CodeUnits.Count == 0 ? "None" : string.Join(", ", r.CodeUnits.Select(k => $"{k.Key} x{k.Value}")),
        Status = r.DocumentationGaps == 0
            ? new BadgeViewModel("success", "Documentation complete", "ri-checkbox-circle-line")
            : new BadgeViewModel("warning", $"{r.DocumentationGaps} {(r.DocumentationGaps == 1 ? "gap" : "gaps")} to resolve", "ri-error-warning-line")
    };
}
