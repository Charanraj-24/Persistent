namespace Reflect.Domain.ProviderDashboard.Enums;

public enum ReflectPlatform
{
    ReflectXR = 1,
    ReflectMobile = 2
}

/// <summary>Where a session happened. Only remote sessions count toward RTM data days.</summary>
public enum SessionSetting
{
    Remote = 1,
    InClinic = 2
}

public enum PatientStatus
{
    Active = 1,
    Paused = 2,
    Discharged = 3
}

public enum AssessmentInstrument
{
    Phq9 = 1,
    Gad7 = 2
}

public enum GoalStatus
{
    Active = 1,
    Achieved = 2,
    OnHold = 3
}

/// <summary>Whether a lower or a higher measured value means progress.</summary>
public enum GoalDirection
{
    Decrease = 1,
    Increase = 2
}

public enum CommunicationChannel
{
    VideoCall = 1,
    PhoneCall = 2,
    SecureMessage = 3,
    InPersonVisit = 4
}

public enum MonitoringTimeSource
{
    /// <summary>Recorded by the dashboard while the provider had the patient's record open.</summary>
    AutoTracked = 1,
    /// <summary>Entered by the provider for monitoring work done outside the dashboard.</summary>
    ManualEntry = 2,
    /// <summary>Delivered by another system (for example the platform's own usage tracking) through the repository.</summary>
    ExternalFeed = 3
}

public enum AttentionSeverity
{
    Positive = 0,
    Info = 1,
    Warning = 2,
    Critical = 3
}
