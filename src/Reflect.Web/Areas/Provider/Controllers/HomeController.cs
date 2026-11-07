using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class HomeController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IMoodCheckInRepository _checkIns;
    private readonly IAssessmentResultRepository _assessments;
    private readonly IRtmTimeEntryRepository _timeEntries;

    public HomeController(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries)
    {
        _currentUser = currentUser;
        _providers = providers;
        _clients = clients;
        _sessions = sessions;
        _checkIns = checkIns;
        _assessments = assessments;
        _timeEntries = timeEntries;
    }

    public IActionResult Index() => RedirectToAction(nameof(Dashboard));

    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        var vm = new ProviderDashboardViewModel();
        await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        return View(vm);
    }

    /// <summary>AJAX: the KPI tiles, refreshed when new client data arrives.</summary>
    public async Task<IActionResult> Kpis(CancellationToken ct)
    {
        var vm = new ProviderDashboardViewModel();
        await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        return PartialView("_DashboardKpis", vm);
    }

    /// <summary>AJAX: one dashboard tab, loaded the first time the provider opens it.</summary>
    public async Task<IActionResult> Tab(string name, CancellationToken ct)
    {
        switch (name)
        {
            case "attention":
            {
                var vm = new DashboardAttentionTabViewModel();
                await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
                return PartialView("_TabAttention", vm);
            }
            case "todos":
            {
                var vm = new DashboardTodosTabViewModel();
                await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
                return PartialView("_TabTodos", vm);
            }
            case "trends":
            {
                var vm = new DashboardTrendsTabViewModel();
                await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
                return PartialView("_TabTrends", vm);
            }
            case "usage":
            {
                var vm = new DashboardUsageTabViewModel();
                await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
                return PartialView("_TabUsage", vm);
            }
            case "outcomes":
            {
                var vm = new DashboardOutcomesTabViewModel();
                await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
                return PartialView("_TabOutcomes", vm);
            }
            case "activity":
            {
                var vm = new DashboardActivityTabViewModel();
                await vm.PrepareData(_currentUser, _providers, _clients, _sessions, ct);
                return PartialView("_TabActivity", vm);
            }
            default:
                return NotFound();
        }
    }
}
