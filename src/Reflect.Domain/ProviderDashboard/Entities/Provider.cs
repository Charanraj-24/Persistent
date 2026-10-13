using Reflect.Domain.Common;

namespace Reflect.Domain.ProviderDashboard.Entities;

public class Provider : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Credentials { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;
    public string Npi { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrEmpty(Credentials) ? $"{FirstName} {LastName}" : $"{FirstName} {LastName}, {Credentials}";
}
