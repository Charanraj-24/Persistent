using Reflect.Domain.Helpers;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Audit;
using Reflect.Domain.Entities.ProviderDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Assistant;
using Reflect.Web.Services.Assistant;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>AJAX: the clinical assistant (chat about one client or the whole caseload).</summary>
[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class AssistantController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IMoodCheckInRepository _checkIns;
    private readonly IAssessmentResultRepository _assessments;
    private readonly IObservationRepository _observations;
    private readonly IRtmTimeEntryRepository _timeEntries;
    private readonly IClinicalAssistant _assistant;
    private readonly ILogger<AssistantController> _log;
    private readonly bool _aiConfigured;
    private readonly AssistantChatStore _chats;

    public AssistantController(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments,
        IObservationRepository observations, IRtmTimeEntryRepository timeEntries, IClinicalAssistant assistant, ILogger<AssistantController> log,
        Microsoft.Extensions.Options.IOptions<AssistantOptions> options, AssistantChatStore chats)
    {
        _chats = chats;
        _aiConfigured = options.Value.IsConfigured;
        _currentUser = currentUser;
        _providers = providers;
        _clients = clients;
        _sessions = sessions;
        _checkIns = checkIns;
        _assessments = assessments;
        _observations = observations;
        _timeEntries = timeEntries;
        _assistant = assistant;
        _log = log;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditAccess(AuditAction.AskAssistant)]
    public async Task<IActionResult> Ask(AssistantAskViewModel vm, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var prior = _chats.Get(userId, vm.ClientId);
        if (!await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _observations, _timeEntries, prior, ct))
            return BadRequest(new { ok = false, error = vm.Error });

        var reply = await _assistant.AskAsync(vm.Prompt, ct);
        var answer = vm.Pseudonymizer.Restore(reply.Answer);
        var now = DateTimeOffset.UtcNow;
        _chats.Append(userId, vm.ClientId,
            new AssistantChatMessage("user", vm.Question.Trim(), now, false, false, string.Empty),
            new AssistantChatMessage("assistant", answer, now, reply.IsOffline, reply.IsError, reply.Model ?? string.Empty));

        // Audit metadata only. Question and answer text are not logged.
        _log.LogInformation("Assistant query by user {UserId} about {Scope}: question {QuestionLength} chars, offline={Offline}, error={Error}.",
            _currentUser.UserId, vm.ClientId.HasValue ? "client " + vm.ClientId : "caseload", vm.Question.Length, reply.IsOffline, reply.IsError);

        return Json(new
        {
            ok = !reply.IsError,
            answer,
            offline = reply.IsOffline,
            model = reply.Model
        });
    }

    /// <summary>AJAX: this provider's saved conversation for the caseload (no clientId) or one client.</summary>
    [HttpGet]
    public async Task<IActionResult> History(Guid? clientId, CancellationToken ct)
    {
        if (clientId.HasValue) await ClientScope.LoadAsync(clientId.Value, _currentUser, _providers, _clients, ct); // ownership check
        var messages = _chats.Get(_currentUser.UserId ?? Guid.Empty, clientId);
        return Json(new
        {
            ok = true,
            messages = messages.Select(m => new { role = m.Role, text = m.Text, offline = m.Offline, error = m.IsError, model = m.Model, at = m.At.ToUsDateTimeString() })
        });
    }

    /// <summary>AJAX: delete this provider's conversation for the caseload or one client.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearHistory(Guid? clientId, CancellationToken ct)
    {
        if (clientId.HasValue) await ClientScope.LoadAsync(clientId.Value, _currentUser, _providers, _clients, ct);
        _chats.Clear(_currentUser.UserId ?? Guid.Empty, clientId);
        return Json(new { ok = true });
    }

    /// <summary>AJAX: draft an RTM note from the client's data (template; polished by the AI when configured).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditAccess(AuditAction.DraftNote)]
    public async Task<IActionResult> DraftNote(AssistantDraftNoteViewModel vm, CancellationToken ct)
    {
        await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _observations, _timeEntries, ct);
        var note = vm.Draft;
        var polished = false;
        if (_aiConfigured)
        {
            var reply = await _assistant.AskAsync(vm.PolishPrompt, ct);
            if (!reply.IsError && !reply.IsOffline && !string.IsNullOrWhiteSpace(reply.Answer))
            {
                note = vm.Pseudonymizer.Restore(reply.Answer);
                polished = true;
            }
        }
        _log.LogInformation("RTM note drafted by user {UserId} for client {ClientId}, ai={Ai}.", _currentUser.UserId, vm.ClientId, polished);
        return Json(new { ok = true, note = note.Length > 1000 ? note[..1000] : note, ai = polished });
    }
}
