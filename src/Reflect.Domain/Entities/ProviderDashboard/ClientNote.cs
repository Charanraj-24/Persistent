namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>What a note is about. A note can be general or attached to one session, questionnaire or piece of artwork.</summary>
public enum NoteTopic
{
    General = 0,
    Session = 1,
    Assessment = 2,
    Artwork = 3,
    Billing = 4,
    Safety = 5
}

/// <summary>
/// A provider's note on a client. Visible only to providers who can open the client (never the Facility Admin),
/// never sent to the AI, and soft-deleted. Only the author can edit or delete it.
/// </summary>
public class ClientNote : BaseEntity
{
    public Guid ProviderClientId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NoteTopic Topic { get; set; }
    /// <summary>The session, assessment or artwork session the note is attached to, if any.</summary>
    public Guid? SubjectId { get; set; }
    /// <summary>Human label for the subject, kept with the note, e.g. "Catharsis, 11/6/2026 8:09 PM".</summary>
    public string SubjectLabel { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public DateTimeOffset? EditedAt { get; set; }

    public const int MaxLength = 2000;
}
