namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>Which Reflect client produced a session.</summary>
public enum ReflectPlatform
{
    ReflectXR = 1,
    ReflectMobile = 2
}

public enum ClientRtmStatus
{
    Active = 1,
    Paused = 2,
    Discharged = 3
}

public enum CheckInPhase
{
    PreSession = 1,
    PostSession = 2
}

public enum AssessmentInstrument
{
    Phq9 = 1,
    Gad7 = 2
}

/// <summary>What the provider was doing during a block of RTM treatment-management time.</summary>
public enum RtmActivityType
{
    DataReview = 1,
    CarePlanUpdate = 2,
    InteractiveCommunication = 3,
    CareCoordination = 4
}

/// <summary>How a time entry was captured.</summary>
public enum RtmTimeSource
{
    /// <summary>Captured by the dashboard's active-time tracker while the provider viewed the client.</summary>
    AutoTracked = 1,
    /// <summary>Entered by the provider.</summary>
    Manual = 2
}

/// <summary>
/// Mode of a provider-client communication. CPT treatment-management codes require at least one
/// real-time, synchronous (audio or audio-video) interaction per calendar month.
/// </summary>
public enum CommunicationMode
{
    None = 0,
    Video = 1,
    Phone = 2,
    InPerson = 3,
    SecureMessage = 4
}

public enum ObservationCategory
{
    TherapeuticResponse = 1,
    Physiologic = 2
}
