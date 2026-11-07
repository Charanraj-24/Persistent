using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notes;

/// <summary>A small "add a note about this" button, with the number of notes already attached.</summary>
public sealed record NoteButtonViewModel(NoteTopic Topic, Guid? SubjectId, string SubjectLabel, int Count, string Describes)
{
    public static NoteButtonViewModel For(NoteTopic topic, Guid subjectId, string label, IReadOnlyDictionary<Guid, int> counts, string describes) =>
        new(topic, subjectId, label, counts != null && counts.TryGetValue(subjectId, out var c) ? c : 0, describes);
}
