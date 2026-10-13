using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Entities;

public class TreatmentGoal : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    /// <summary>What is measured, for example "PHQ-9 score" or "Sessions per week".</summary>
    public string MeasureName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public GoalDirection Direction { get; set; } = GoalDirection.Decrease;
    public decimal BaselineValue { get; set; }
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly TargetDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public DateTimeOffset? DateUpdated { get; set; }
}
