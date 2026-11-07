using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Notifications;
using Reflect.Web.Services.Notifications;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>Header bell for providers. Count is polled (and does not extend the sign-in session); the list loads when the bell opens.</summary>
[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class NotificationsController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly NotificationReadState _readState;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IMoodCheckInRepository _checkIns;
    private readonly IAssessmentResultRepository _assessments;
    private readonly IRtmTimeEntryRepository _timeEntries;

    public NotificationsController(ICurrentUserService currentUser, NotificationReadState readState, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments,
        IRtmTimeEntryRepository timeEntries)
    {
        _currentUser = currentUser;
        _readState = readState;
        _providers = providers;
        _clients = clients;
        _sessions = sessions;
        _checkIns = checkIns;
        _assessments = assessments;
        _timeEntries = timeEntries;
    }

    private async Task<NotificationsViewModel> LoadAsync(CancellationToken ct)
    {
        var vm = new NotificationsViewModel();
        await vm.PrepareData(_currentUser, _readState, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        return vm;
    }

    [HttpGet]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var vm = await LoadAsync(ct);
        return Json(new { unread = vm.Unread, urgent = vm.UnreadUrgent });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => PartialView("_NotificationList", await LoadAsync(ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var vm = await LoadAsync(ct);
        _readState.MarkRead(_currentUser.UserId ?? Guid.Empty, vm.Items.Select(i => i.Key));
        return Json(new { unread = 0, urgent = 0 });
    }
}
