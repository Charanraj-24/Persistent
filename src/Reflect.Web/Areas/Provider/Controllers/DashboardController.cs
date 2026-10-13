using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Dashboard;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderDashboardPolicies.ProviderOnly)]
public class DashboardController : Controller
{
    private readonly ProviderDashboardRepositories _repos;
    private readonly ProviderContext _context;

    public DashboardController(ProviderDashboardRepositories repos, ProviderContext context)
    {
        _repos = repos;
        _context = context;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new DashboardIndexViewModel();
        await vm.PrepareData(_repos, _context, ct);
        return View(vm);
    }
}
