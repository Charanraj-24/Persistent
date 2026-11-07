namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>
/// A record that new client data arrived (from the Reflect apps' API or the demo simulator).
/// Drives the live feed on open dashboard pages and doubles as an ingestion audit trail.
/// </summary>
public class IngestEvent : BaseEntity
{
    public Guid ClientUserId { get; set; }
    /// <summary>session, assessment, observation.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Short provider-facing description, for example "Finished Creative Calm in Reflect XR (18 min)".</summary>
    public string Summary { get; set; } = string.Empty;
    /// <summary>"api" or "simulator".</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>True when the event raised an urgent clinical flag (for example PHQ-9 item 9).</summary>
    public bool IsUrgent { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
}
