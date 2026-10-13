using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Entities;

/// <summary>
/// A provider-documented contact with the patient that happened outside the dashboard
/// (video, phone, secure message). The dashboard documents it; it does not carry the conversation.
/// </summary>
public class CareCommunication : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid ProviderId { get; set; }
    public CommunicationChannel Channel { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Summary { get; set; } = string.Empty;
    public bool DiscussedRemoteData { get; set; }

    /// <summary>Real-time, two-way communication (video or phone). Required in the month for RTM treatment-management codes.</summary>
    public bool IsInteractive => Channel is CommunicationChannel.VideoCall or CommunicationChannel.PhoneCall;
}
