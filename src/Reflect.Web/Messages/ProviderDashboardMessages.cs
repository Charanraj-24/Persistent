using System.Globalization;

namespace Reflect.Web.Messages;

public static class TempDataKeys
{
    public const string Success = "SuccessMessage";
    public const string Error = "ErrorMessage";
    public const string Info = "InfoMessage";
}

/// <summary>All user-facing toast and notification text for the Provider Dashboard, in one place.</summary>
public static class ProviderDashboardMessages
{
    public static string SignedInAs(string name) => $"You're now viewing the dashboard as {name}.";
    public const string GoalCreated = "Treatment goal added.";
    public const string GoalUpdated = "Treatment goal updated.";
    public const string CommunicationLogged = "Communication logged. It now counts toward this month's RTM documentation.";
    public static string ManualTimeLogged(int minutes) => string.Format(CultureInfo.InvariantCulture, "{0} minute(s) of monitoring time logged.", minutes);
    public const string TrackingPaused = "Monitoring time paused.";
    public const string TrackingResumed = "Monitoring time resumed.";
    public const string TrackingSaveFailed = "We couldn't save monitoring time. It will retry automatically.";
    public const string PatientNotFound = "That patient isn't on your caseload.";
    public const string GoalNotFound = "That goal wasn't found.";
    public const string MonitoringEntryNotFound = "That monitoring session wasn't found.";
}
