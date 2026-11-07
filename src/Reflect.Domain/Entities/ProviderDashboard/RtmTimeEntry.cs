namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>
/// CLINICAL / BILLING. A block of provider treatment-management time for one client.
/// Only attested entries count toward CPT 98979 / 98980 / 98981.
/// </summary>
public class RtmTimeEntry : BaseEntity
{
    public Guid ProviderId { get; set; }
    public Guid ClientId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    /// <summary>Active seconds (idle time and hidden-tab time excluded for auto-tracked entries).</summary>
    public int ActiveSeconds { get; set; }
    public RtmActivityType Activity { get; set; }
    public RtmTimeSource Source { get; set; }
    public CommunicationMode CommunicationMode { get; set; } = CommunicationMode.None;
    public string Note { get; set; } = string.Empty;
    /// <summary>Client-generated key that ties heartbeats from one viewing session to one entry.</summary>
    public string TrackingKey { get; set; } = string.Empty;
    public DateTimeOffset LastHeartbeatAt { get; set; }
    public bool IsAttested { get; set; }
    public DateTimeOffset? AttestedAt { get; set; }

    public double Minutes => ActiveSeconds / 60.0;

    /// <summary>True when this entry documents a real-time interaction that satisfies the CPT requirement.</summary>
    public bool IsRealTimeInteraction =>
        Activity == RtmActivityType.InteractiveCommunication &&
        (CommunicationMode == CommunicationMode.Video ||
         CommunicationMode == CommunicationMode.Phone ||
         CommunicationMode == CommunicationMode.InPerson);
}
