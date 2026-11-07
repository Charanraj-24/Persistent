namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>CLINICAL. A standardized measure (PHQ-9 or GAD-7) completed by the client in-app.</summary>
public class AssessmentResult : BaseEntity
{
    public Guid ClientUserId { get; set; }
    public AssessmentInstrument Instrument { get; set; }
    public int TotalScore { get; set; }
    /// <summary>PHQ-9 item 9 (thoughts of self-harm), 0-3. Always 0 for GAD-7.</summary>
    public int SafetyItemScore { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    /// <summary>How the record arrived: "api", "phone", "live demo" or "simulator". Empty for imported history.</summary>
    public string Source { get; set; } = string.Empty;
}
