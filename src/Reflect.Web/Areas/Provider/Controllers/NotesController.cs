using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Notes;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Audit;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>Provider notes on a client (AJAX). Ownership is checked on every call; only the author can edit, pin or delete.</summary>
[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class NotesController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IClientNoteRepository _notes;
    private readonly AuditLogger _audit;

    public NotesController(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IClientNoteRepository notes, AuditLogger audit)
    {
        _currentUser = currentUser;
        _providers = providers;
        _clients = clients;
        _notes = notes;
        _audit = audit;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(NoteSaveViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { ok = false, error = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Check the note." });
        if (!await vm.PrepareDataForSave(_currentUser, _providers, _clients, _notes, ct))
            return BadRequest(new { ok = false, error = vm.Error });
        await _audit.LogAsync(vm.IsEdit ? AuditAction.EditNote : AuditAction.AddNote, vm.ClientId, NoteTopics.Label(vm.Topic), ct: ct);
        return Json(new { ok = true, message = vm.IsEdit ? "Note updated." : "Note saved." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid clientId, Guid id, CancellationToken ct)
    {
        var note = await OwnNoteAsync(clientId, id, ct);
        if (note == null) return BadRequest(new { ok = false, error = "Only the author can delete this note." });
        note.IsDeleted = true;
        note.DateDeleted = DateTimeOffset.UtcNow;
        await _notes.UpdateAsync(note, ct);
        await _audit.LogAsync(AuditAction.DeleteNote, clientId, NoteTopics.Label(note.Topic), ct: ct);
        return Json(new { ok = true, message = "Note deleted." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Pin(Guid clientId, Guid id, bool pinned, CancellationToken ct)
    {
        var note = await OwnNoteAsync(clientId, id, ct);
        if (note == null) return BadRequest(new { ok = false, error = "Only the author can pin this note." });
        note.IsPinned = pinned;
        await _notes.UpdateAsync(note, ct);
        return Json(new { ok = true, message = pinned ? "Pinned to the top." : "Unpinned." });
    }

    private async Task<ClientNote> OwnNoteAsync(Guid clientId, Guid id, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, _currentUser, _providers, _clients, ct);
        var note = await _notes.GetAsync(id, ct);
        return note != null && note.ProviderClientId == scope.Client.Id && note.AuthorUserId == (_currentUser.UserId ?? Guid.Empty) ? note : null;
    }
}
