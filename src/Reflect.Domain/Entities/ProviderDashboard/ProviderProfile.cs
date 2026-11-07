namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>A licensed clinician who monitors Reflect clients. Links to the local user id.</summary>
public class ProviderProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Credentials { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    /// <summary>Synthetic NPI for the demo. Never a real identifier.</summary>
    public string Npi { get; set; } = string.Empty;
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
}
