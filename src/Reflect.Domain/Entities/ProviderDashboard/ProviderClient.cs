namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>
/// A Reflect end user enrolled in Remote Therapeutic Monitoring under a provider.
/// Clinical context lives here; app usage lives in <see cref="AppSession"/> (product analytics).
/// </summary>
public class ProviderClient : BaseEntity
{
    public Guid ProviderId { get; set; }
    /// <summary>The Reflect end user's local user id (joins to analytics data).</summary>
    public Guid ClientUserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    /// <summary>Synthetic medical record number.</summary>
    public string Mrn { get; set; } = string.Empty;
    public string PrimaryDiagnosisCode { get; set; } = string.Empty;
    public string PrimaryDiagnosisLabel { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string TreatmentGoals { get; set; } = string.Empty;
    /// <summary>Date the client gave consent for RTM (required before billing).</summary>
    public DateOnly? RtmConsentDate { get; set; }
    /// <summary>Start of the first 30-day device-supply period.</summary>
    public DateOnly RtmStartDate { get; set; }
    /// <summary>True once CPT 98975 (initial set-up and education) has been billed for this episode.</summary>
    public bool SetupBilled { get; set; }
    public ClientRtmStatus Status { get; set; } = ClientRtmStatus.Active;

    public string FullName => $"{FirstName} {LastName}";
    public string Initials => $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}{(LastName.Length > 0 ? LastName[0] : ' ')}".Trim();
}
