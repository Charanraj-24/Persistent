using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Audit;

public static class AuditLabels
{
    public static string For(AuditAction a) => a switch
    {
        AuditAction.SignIn => "Signed in",
        AuditAction.SignInFailed => "Sign-in failed",
        AuditAction.SignOut => "Signed out",
        AuditAction.ViewClient => "Opened client record",
        AuditAction.ViewClientTab => "Viewed client tab",
        AuditAction.ViewSessionPrep => "Opened session prep",
        AuditAction.PrintRtmDocumentation => "Opened RTM documentation",
        AuditAction.LogTime => "Logged RTM time",
        AuditAction.AttestReviewTime => "Attested review time",
        AuditAction.ExportReport => "Exported report",
        AuditAction.AskAssistant => "Asked AI assistant",
        AuditAction.DraftNote => "Drafted RTM note",
        AuditAction.ViewBilling => "Viewed billing",
        _ => a.ToString()
    };

    public static string Icon(AuditAction a) => a switch
    {
        AuditAction.SignIn or AuditAction.SignOut => "ri-login-box-line",
        AuditAction.SignInFailed => "ri-error-warning-line",
        AuditAction.ExportReport or AuditAction.PrintRtmDocumentation => "ri-download-2-line",
        AuditAction.AskAssistant or AuditAction.DraftNote => "ri-sparkling-2-line",
        AuditAction.LogTime or AuditAction.AttestReviewTime => "ri-timer-line",
        AuditAction.ViewBilling => "ri-money-dollar-box-line",
        _ => "ri-eye-line"
    };

    /// <summary>Pseudonymous record reference for admin views: lets a privacy officer correlate events without seeing who the client is.</summary>
    public static string RecordRef(Guid? clientId) => clientId.HasValue ? "Record " + clientId.Value.ToString("N")[..6].ToUpperInvariant() : "-";
}
