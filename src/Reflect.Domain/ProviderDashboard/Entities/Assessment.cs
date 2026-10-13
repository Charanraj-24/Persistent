using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Entities;

/// <summary>A standardized self-report measure (PHQ-9 or GAD-7) completed in the app or in clinic.</summary>
public class Assessment : BaseEntity
{
    public Guid PatientId { get; set; }
    public AssessmentInstrument Instrument { get; set; }
    public int Score { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public ReflectPlatform? CompletedOn { get; set; }
}
