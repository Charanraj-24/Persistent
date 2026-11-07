using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Live;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Ingestion;
using Reflect.Web.Services.Simulation;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>Live data: the feed every Provider page polls, and the demo page that generates app events.</summary>
[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class LiveController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IIngestEventRepository _events;
    private readonly IngestionService _ingestion;
    private readonly LiveDemoState _state;
    private readonly IConfiguration _config;
    private readonly Reflect.Web.Services.CheckIn.PhoneCheckInService _checkIns;

    public LiveController(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IIngestEventRepository events, IngestionService ingestion, LiveDemoState state, IConfiguration config,
        Reflect.Web.Services.CheckIn.PhoneCheckInService checkIns)
    {
        _currentUser = currentUser;
        _providers = providers;
        _clients = clients;
        _events = events;
        _ingestion = ingestion;
        _state = state;
        _config = config;
        _checkIns = checkIns;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new LiveDemoViewModel();
        await vm.PrepareData(_currentUser, _providers, _clients, _events, _state, (_config["Ingest:ApiKey"] ?? string.Empty).Length >= 16, ct);
        return View(vm);
    }

    /// <summary>AJAX: new events since <paramref name="after"/> (Unix ms).</summary>
    [HttpGet]
    public async Task<IActionResult> Feed(long? after, CancellationToken ct)
    {
        var vm = new LiveFeedViewModel();
        await vm.PrepareData(after, _currentUser, _providers, _clients, _events, ct);
        return Json(vm);
    }

    /// <summary>AJAX: run one demo scenario for a client.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunScenario(LiveScenarioViewModel vm, CancellationToken ct)
    {
        var client = await vm.PrepareDataForSave(_currentUser, _providers, _clients, ct);
        if (client == null) return BadRequest(new { ok = false, error = ProviderDashboardMessages.ScenarioUnknown });
        var result = await DemoScenarios.RunAsync(vm.Scenario, client, _ingestion, Random.Shared, "simulator", ct);
        return result.Ok
            ? Json(new { ok = true, summary = $"{client.FullName}: {result.Summary}" })
            : BadRequest(new { ok = false, error = string.Join(" ", result.Errors) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Simulator(bool enable, int intervalSeconds, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(_currentUser, _providers, ct);
        if (enable) _state.Start(provider.Id, intervalSeconds); else _state.Stop();
        TempData["ToastMessage"] = enable ? ProviderDashboardMessages.SimulatorStarted(_state.IntervalSeconds) : ProviderDashboardMessages.SimulatorStopped;
        TempData["ToastType"] = "info";
        return RedirectToAction(nameof(Index));
    }

    // ---- QR audience check-in (demo) ----

    private string PublicBase()
    {
        var configured = (_config["Demo:PublicBaseUrl"] ?? string.Empty).Trim().TrimEnd('/');
        return configured.Length > 0 ? configured : $"{Request.Scheme}://{Request.Host}";
    }

    private object CheckInJson(Reflect.Web.Services.CheckIn.CheckInJoinSession s)
    {
        if (s == null) return new { active = false };
        var url = $"{PublicBase()}/checkin?code={s.Code}";
        var host = new Uri(url).Host;
        return new
        {
            active = true, code = s.Code, url, submissions = s.Submissions, clientId = s.ClientId,
            secondsLeft = (int)Math.Max(0, (s.ExpiresAt - DateTimeOffset.UtcNow).TotalSeconds),
            localOnly = host is "localhost" or "127.0.0.1" or "::1" or "[::1]"
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartCheckIn(Guid clientId, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, _currentUser, _providers, _clients, ct); // ownership check
        var s = _checkIns.Start(_currentUser.UserId ?? Guid.Empty, scope.Client.Id, scope.Client.ClientUserId, DateTimeOffset.UtcNow);
        return Json(CheckInJson(s));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StopCheckIn()
    {
        _checkIns.Stop(_currentUser.UserId ?? Guid.Empty);
        return Json(new { active = false });
    }

    [HttpGet]
    public IActionResult CheckInStatus() => Json(CheckInJson(_checkIns.ActiveFor(_currentUser.UserId ?? Guid.Empty, DateTimeOffset.UtcNow)));
}
