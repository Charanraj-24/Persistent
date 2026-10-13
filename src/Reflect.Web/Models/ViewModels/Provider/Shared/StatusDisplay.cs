using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

/// <summary>A status shown as color + icon + text, never color alone. Color is a theme color name (success, warning, ...).</summary>
public sealed record StatusBadge(string Color, string Icon, string Text);

public static class StatusDisplay
{
    public static StatusBadge Rtm(RtmReadiness readiness) => readiness switch
    {
        RtmReadiness.Ready => new("success", "ri-checkbox-circle-line", "Ready to bill"),
        RtmReadiness.Partial => new("info", "ri-contrast-2-line", "Partially billable"),
        RtmReadiness.OnTrack => new("info", "ri-route-line", "On track"),
        RtmReadiness.BehindPace => new("warning", "ri-speed-line", "Behind pace"),
        RtmReadiness.AtRisk => new("warning", "ri-error-warning-line", "At risk"),
        RtmReadiness.NotBillable => new("danger", "ri-close-circle-line", "Not billable"),
        _ => new("info", "ri-forbid-line", "Not enrolled")
    };

    public static StatusBadge Attention(AttentionSeverity severity) => severity switch
    {
        AttentionSeverity.Critical => new("danger", "ri-alarm-warning-line", "Urgent"),
        AttentionSeverity.Warning => new("warning", "ri-error-warning-line", "Review"),
        AttentionSeverity.Info => new("info", "ri-information-line", "FYI"),
        _ => new("success", "ri-thumb-up-line", "Positive")
    };

    public static StatusBadge Patient(PatientStatus status) => status switch
    {
        PatientStatus.Active => new("success", "ri-pulse-line", "Active"),
        PatientStatus.Paused => new("warning", "ri-pause-circle-line", "Paused"),
        _ => new("info", "ri-archive-line", "Discharged")
    };

    public static StatusBadge Goal(GoalTrack track) => track switch
    {
        GoalTrack.Achieved => new("success", "ri-trophy-line", "Achieved"),
        GoalTrack.OnTrack => new("info", "ri-route-line", "On track"),
        GoalTrack.Behind => new("warning", "ri-hourglass-line", "Behind"),
        _ => new("info", "ri-pause-circle-line", "On hold")
    };

    public static StatusBadge Severity(int level, string label) => level switch
    {
        0 => new("success", "ri-checkbox-blank-circle-line", label),
        1 => new("info", "ri-checkbox-blank-circle-line", label),
        2 => new("warning", "ri-error-warning-line", label),
        _ => new("danger", "ri-alarm-warning-line", label)
    };

    public static StatusBadge Setting(SessionSetting setting) => setting == SessionSetting.Remote
        ? new("success", "ri-home-wifi-line", "Remote")
        : new("info", "ri-hospital-line", "In clinic");

    public static StatusBadge Channel(CommunicationChannel channel) => channel switch
    {
        CommunicationChannel.VideoCall => new("success", "ri-vidicon-line", "Video call"),
        CommunicationChannel.PhoneCall => new("success", "ri-phone-line", "Phone call"),
        CommunicationChannel.SecureMessage => new("info", "ri-message-3-line", "Secure message"),
        _ => new("info", "ri-user-voice-line", "In person")
    };

    public static StatusBadge Source(MonitoringTimeSource source) => source switch
    {
        MonitoringTimeSource.AutoTracked => new("success", "ri-timer-flash-line", "Auto-tracked"),
        MonitoringTimeSource.ManualEntry => new("warning", "ri-edit-2-line", "Manual entry"),
        _ => new("info", "ri-links-line", "External feed")
    };

    public static string PlatformIcon(ReflectPlatform platform) => platform == ReflectPlatform.ReflectXR ? "ri-goggles-line" : "ri-smartphone-line";

    public static string FormatMinutes(int minutes) => minutes < 60 ? $"{minutes} min" : $"{minutes / 60} h {minutes % 60} min";

    public static string FormatDuration(int seconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
        return ts.TotalHours >= 1 ? $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}" : $"{ts.Minutes}:{ts.Seconds:00}";
    }

    public static string RelativeDays(int days) => days switch
    {
        int.MaxValue => "Never",
        0 => "Today",
        1 => "Yesterday",
        _ => $"{days} days ago"
    };
}
