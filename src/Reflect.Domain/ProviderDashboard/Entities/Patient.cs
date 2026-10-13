using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Entities;

public class Patient : BaseEntity
{
    public Guid ProviderId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PreferredName { get; set; } = string.Empty;
    public string Pronouns { get; set; } = string.Empty;
    /// <summary>Chart number shown to the provider. Synthetic in the demo.</summary>
    public string ChartNumber { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string PrimaryConcern { get; set; } = string.Empty;
    public PatientStatus Status { get; set; } = PatientStatus.Active;
    public DateOnly EnrolledOn { get; set; }
    /// <summary>Date the patient consented to Remote Therapeutic Monitoring. Empty if not consented.</summary>
    public DateOnly? RtmConsentOn { get; set; }
    public bool UsesReflectXR { get; set; }
    public bool UsesReflectMobile { get; set; }
    /// <summary>True when a heart-rate capable device feeds physiologic data (RPM preview).</summary>
    public bool HasPhysiologicDevice { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public string Initials => $"{FirstName.FirstOrDefault()}{LastName.FirstOrDefault()}";

    public int AgeOn(DateOnly day)
    {
        var age = day.Year - DateOfBirth.Year;
        if (DateOfBirth > day.AddYears(-age)) age--;
        return age;
    }
}
