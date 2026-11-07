namespace Reflect.Domain.Entities.ProviderDashboard;

public enum AuditAction
{
    SignIn = 1,
    SignInFailed = 2,
    SignOut = 3,
    ViewClient = 10,
    ViewClientTab = 11,
    ViewSessionPrep = 12,
    PrintRtmDocumentation = 13,
    LogTime = 20,
    AttestReviewTime = 21,
    ExportReport = 30,
    AskAssistant = 40,
    DraftNote = 41,
    ViewBilling = 50,
    AddNote = 60,
    EditNote = 61,
    DeleteNote = 62
}

/// <summary>
/// HIPAA access audit: who opened which client's record, and when. Holds no clinical content (no scores, notes or names
/// of clients), so the Facility Admin can review it without seeing patient data. Append-only.
/// </summary>
public class AccessAuditEntry : BaseEntity
{
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    /// <summary>ProviderClient id, when the action touched one client's record.</summary>
    public Guid? ClientId { get; set; }
    /// <summary>Non-clinical detail such as the tab name or report month.</summary>
    public string Detail { get; set; } = string.Empty;
    public bool Succeeded { get; set; } = true;
}
