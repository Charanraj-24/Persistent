using System.ComponentModel.DataAnnotations;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notes;

/// <summary>Create or edit a note (AJAX). Only the author can edit; the client must belong to the provider.</summary>
public class NoteSaveViewModel
{
    public Guid ClientId { get; set; }
    /// <summary>Empty to create a note; set to edit one.</summary>
    public Guid? NoteId { get; set; }

    [Required(ErrorMessage = "Write the note first.")]
    [StringLength(ClientNote.MaxLength, ErrorMessage = "Notes can be up to 2,000 characters.")]
    public string Body { get; set; } = string.Empty;

    public NoteTopic Topic { get; set; }
    public Guid? SubjectId { get; set; }

    [StringLength(120)]
    public string SubjectLabel { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    public string Error { get; private set; } = string.Empty;
    public bool IsEdit => NoteId.HasValue;

    public async Task<bool> PrepareDataForSave(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IClientNoteRepository notes, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(ClientId, currentUser, providers, clients, ct); // throws if not this provider's client
        var me = currentUser.UserId ?? Guid.Empty;
        var body = (Body ?? string.Empty).Trim();
        if (body.Length == 0) { Error = "Write the note first."; return false; }
        if (!Enum.IsDefined(typeof(NoteTopic), Topic)) Topic = NoteTopic.General;

        if (NoteId.HasValue)
        {
            var existing = await notes.GetAsync(NoteId.Value, ct);
            if (existing == null || existing.ProviderClientId != scope.Client.Id) { Error = "That note no longer exists."; return false; }
            if (existing.AuthorUserId != me) { Error = "Only the author can edit this note."; return false; }
            existing.Body = body;
            existing.IsPinned = IsPinned;
            existing.EditedAt = DateTimeOffset.UtcNow;
            await notes.UpdateAsync(existing, ct);
            return true;
        }

        await notes.AddAsync(new ClientNote
        {
            ProviderClientId = scope.Client.Id, AuthorUserId = me, AuthorName = string.IsNullOrEmpty(scope.Provider.Credentials) ? scope.Provider.FullName : $"{scope.Provider.FullName}, {scope.Provider.Credentials.Split(',')[0].Trim()}",
            Body = body, Topic = Topic, SubjectId = SubjectId, SubjectLabel = (SubjectLabel ?? string.Empty).Trim(), IsPinned = IsPinned
        }, ct);
        return true;
    }
}
