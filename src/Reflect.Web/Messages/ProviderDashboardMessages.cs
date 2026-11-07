namespace Reflect.Web.Messages;

/// <summary>All user-facing message text for the Provider area (toasts, confirmations, validation).</summary>
public static class ProviderDashboardMessages
{
    public const string TimeEntrySaved = "Time entry saved and attested.";
    public const string InteractionSaved = "Live interaction logged.";
    public static string PendingAttested(int minutes) => $"{minutes} {(minutes == 1 ? "minute" : "minutes")} of review time attested.";
    public const string NothingToAttest = "There is no pending review time to attest.";
    public const string PendingDiscarded = "Pending review time discarded.";
    public const string TimeEntryNotPending = "Only pending review time can be discarded.";

    public const string ValidationFutureDate = "The date can't be in the future.";
    public const string ValidationOldDate = "Time can only be logged for this month or last month.";
    public const string ValidationModeRequired = "Choose how you communicated with the client.";
    public const string ValidationModeNotLive = "Secure messages don't count as a live interaction. Log them as care coordination instead.";

    public const string ConfirmAttestTitle = "Attest review time?";
    public static string ConfirmAttestText(int minutes) =>
        $"You confirm that the {minutes} pending {(minutes == 1 ? "minute was" : "minutes were")} spent reviewing this client's monitoring data.";
    public const string ConfirmDiscardTitle = "Discard pending time?";
    public const string ConfirmDiscardText = "This pending review time will not count toward billing.";

    public const string TrackerPaused = "Review timer paused";
    public const string TrackerActive = "Tracking review time";
    public const string TrackerIdle = "Idle, timer paused";
    public const string TabLoadFailed = "This section could not be loaded.";
    public static string SimulatorStarted(int seconds) => $"Simulator started: a new event about every {seconds} seconds.";
    public const string SimulatorStopped = "Simulator stopped.";
    public const string ScenarioUnknown = "That scenario is not available.";
    public const string LiveNewData = "New client data arrived.";
}
