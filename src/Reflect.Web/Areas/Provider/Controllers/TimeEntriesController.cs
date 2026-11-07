using Reflect.Web.Services.Audit;
using Reflect.Domain.Entities.ProviderDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.ProviderDashboard.TimeEntries;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class TimeEntriesController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IRtmTimeEntryRepository _timeEntries;

    public TimeEntriesController(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IRtmTimeEntryRepository timeEntries)
    {
        _currentUser = currentUser;
        _providers = providers;
        _clients = clients;
        _timeEntries = timeEntries;
    }

    public async Task<IActionResult> Create(Guid clientId, RtmActivityType? activity, CancellationToken ct)
    {
        var vm = new TimeEntryCreateViewModel();
        await vm.PrepareData(clientId, activity, _currentUser, _providers, _clients, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditAccess(AuditAction.LogTime)]
    public async Task<IActionResult> Create(TimeEntryCreateViewModel vm, CancellationToken ct)
    {
        var save = await vm.PrepareDataForSave(_currentUser, _providers, _clients, ModelState, ct);
        if (save == null) return View(vm);

        await _timeEntries.AddAsync(save.Entry, ct);
        TempData["ToastMessage"] = save.Entry.Activity == RtmActivityType.InteractiveCommunication
            ? ProviderDashboardMessages.InteractionSaved
            : ProviderDashboardMessages.TimeEntrySaved;
        TempData["ToastType"] = "success";
        return Redirect(Url.Action("Details", "Clients", new { id = vm.ClientId }) + "#tab-rtm");
    }

    /// <summary>AJAX: one heartbeat from the active-time tracker.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Heartbeat(ReviewHeartbeatViewModel vm, CancellationToken ct)
    {
        var save = await vm.PrepareDataForSave(_currentUser, _providers, _clients, _timeEntries, ct);
        if (save == null) return Json(new { ok = false, reset = true });

        if (save.IsNew) await _timeEntries.AddAsync(save.Entry, ct);
        else await _timeEntries.UpdateAsync(save.Entry, ct);
        return Json(new { ok = true, accepted = save.AcceptedSeconds, total = save.Entry.ActiveSeconds });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuditAccess(AuditAction.AttestReviewTime)]
    public async Task<IActionResult> AttestPending(ReviewTimeActionViewModel vm, CancellationToken ct)
    {
        var save = await vm.PrepareDataForAttest(_currentUser, _providers, _clients, _timeEntries, ct);
        foreach (var e in save.Entries) await _timeEntries.UpdateAsync(e, ct);

        TempData["ToastMessage"] = save.Entries.Count == 0
            ? ProviderDashboardMessages.NothingToAttest
            : ProviderDashboardMessages.PendingAttested(save.Seconds / 60);
        TempData["ToastType"] = save.Entries.Count == 0 ? "info" : "success";
        return Redirect(Url.Action("Details", "Clients", new { id = vm.ClientId }) + "#tab-rtm");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DiscardPending(ReviewTimeActionViewModel vm, CancellationToken ct)
    {
        var save = await vm.PrepareDataForDiscard(_currentUser, _providers, _clients, _timeEntries, ct);
        if (save != null) foreach (var e in save.Entries) await _timeEntries.UpdateAsync(e, ct);

        TempData["ToastMessage"] = save == null ? ProviderDashboardMessages.TimeEntryNotPending : ProviderDashboardMessages.PendingDiscarded;
        TempData["ToastType"] = save == null ? "warning" : "success";
        return Redirect(Url.Action("Details", "Clients", new { id = vm.ClientId }) + "#tab-rtm");
    }
}
