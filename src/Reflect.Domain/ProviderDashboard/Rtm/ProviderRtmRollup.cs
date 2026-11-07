namespace Reflect.Domain.ProviderDashboard.Rtm;

/// <summary>One client's month, as input to a provider-level rollup. Carries no identity.</summary>
public sealed record ClientMonthResult(RtmEvaluation Evaluation, IReadOnlyList<BillableCode> MonthCodes, bool HasConsent);

/// <summary>
/// Billing totals for one provider and month, built only from counts. Used by the Facility Admin view, which must not
/// see client names or clinical data (HIPAA minimum necessary).
/// </summary>
public sealed class ProviderRtmRollup
{
    public int ClientsOnRtm { get; init; }
    public int ClientsWithCodes { get; init; }
    public int AttestedMinutes { get; init; }
    public int PendingMinutes { get; init; }
    public int ClientsWithLiveInteraction { get; init; }
    /// <summary>Clients with at least 10 attested minutes but no live interaction (management codes withheld).</summary>
    public int ClientsMissingLiveInteraction { get; init; }
    public int ClientsMissingConsent { get; init; }
    /// <summary>Clients with any pending (unattested) review time.</summary>
    public int ClientsWithPendingTime { get; init; }
    public IReadOnlyDictionary<string, int> CodeUnits { get; init; } = new Dictionary<string, int>();

    public int DocumentationGaps => ClientsMissingLiveInteraction + ClientsMissingConsent + ClientsWithPendingTime;

    public static ProviderRtmRollup From(IEnumerable<ClientMonthResult> clients)
    {
        var list = clients.ToList();
        var units = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var code in list.SelectMany(c => c.MonthCodes))
            units[code.Code] = units.GetValueOrDefault(code.Code) + code.Units;

        return new ProviderRtmRollup
        {
            ClientsOnRtm = list.Count,
            ClientsWithCodes = list.Count(c => c.MonthCodes.Count > 0),
            AttestedMinutes = list.Sum(c => c.Evaluation.ManagementMinutes),
            PendingMinutes = list.Sum(c => c.Evaluation.PendingMinutes),
            ClientsWithLiveInteraction = list.Count(c => c.Evaluation.HasRealTimeInteraction),
            ClientsMissingLiveInteraction = list.Count(c => !c.Evaluation.HasRealTimeInteraction &&
                                                            c.Evaluation.ManagementMinutes >= RtmBillingRules.MinMinutesShortManagement),
            ClientsMissingConsent = list.Count(c => !c.HasConsent),
            ClientsWithPendingTime = list.Count(c => c.Evaluation.PendingMinutes > 0),
            CodeUnits = units
        };
    }
}
