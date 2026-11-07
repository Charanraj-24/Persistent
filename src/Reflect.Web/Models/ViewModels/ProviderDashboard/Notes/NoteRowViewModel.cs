using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notes;

public class NoteRowViewModel
{
    public Guid Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string When { get; set; } = string.Empty;
    public string Edited { get; set; } = string.Empty;
    public NoteTopic Topic { get; set; }
    public string TopicLabel { get; set; } = string.Empty;
    public string TopicIcon { get; set; } = string.Empty;
    public string SubjectLabel { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool CanEdit { get; set; }

    public static NoteRowViewModel From(ClientNote n, Guid currentUserId) => new()
    {
        Id = n.Id, Body = n.Body, Author = n.AuthorName, When = n.DateCreated.ToUsDateTimeString(),
        Edited = n.EditedAt.HasValue ? "Edited " + n.EditedAt.Value.ToUsDateTimeString() : string.Empty,
        Topic = n.Topic, TopicLabel = NoteTopics.Label(n.Topic), TopicIcon = NoteTopics.Icon(n.Topic),
        SubjectLabel = n.SubjectLabel, IsPinned = n.IsPinned, CanEdit = n.AuthorUserId == currentUserId
    };
}
