using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Entities;

/// <summary>Time a provider spent reviewing one patient's remote data. This is the time RTM treatment management bills for.</summary>
public class MonitoringTimeEntry : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Guid PatientId { get; set; }
    public MonitoringTimeSource Source { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    /// <summary>Seconds of active review. Idle and hidden-tab time are excluded.</summary>
    public int ActiveSeconds { get; set; }
    /// <summary>What was reviewed (required for manual entries).</summary>
    public string Note { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
}
