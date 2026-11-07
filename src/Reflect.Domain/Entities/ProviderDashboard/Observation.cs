namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>
/// CLINICAL. A single measured value in a generic, FHIR-Observation-like shape (code + value + unit + time).
/// New physiologic measures (heart rate today; HRV, SpO2, sleep later) are new codes, not new tables,
/// so Remote Physiologic Monitoring can be added without rebuilding the platform.
/// </summary>
public class Observation : BaseEntity
{
    public Guid ClientUserId { get; set; }
    public ObservationCategory Category { get; set; }
    /// <summary>LOINC code when one exists, for example 8867-4 (heart rate).</summary>
    public string Code { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset EffectiveAt { get; set; }
    /// <summary>Where the value came from, for example "Paired wearable".</summary>
    public string SourceDevice { get; set; } = string.Empty;
    /// <summary>Optional link to the app session the reading belongs to.</summary>
    public Guid? AppSessionId { get; set; }
    /// <summary>"pre" or "post" when tied to a session; empty otherwise.</summary>
    public string Context { get; set; } = string.Empty;
    /// <summary>How the record arrived: "api", "phone", "live demo" or "simulator". Empty for imported history.</summary>
    public string Source { get; set; } = string.Empty;
}
