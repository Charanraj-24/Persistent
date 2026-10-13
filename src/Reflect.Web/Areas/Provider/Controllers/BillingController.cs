using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Billing;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderDashboardPolicies.ProviderOnly)]
public class BillingController : Controller
{
    private readonly ProviderDashboardRepositories _repos;
    private readonly ProviderContext _context;

    public BillingController(ProviderDashboardRepositories repos, ProviderContext context)
    {
        _repos = repos;
        _context = context;
    }

    public async Task<IActionResult> Index(string period, CancellationToken ct)
    {
        var vm = new BillingIndexViewModel();
        await vm.PrepareData(_repos, _context, period, ct);
        return View(vm);
    }

    public async Task<IActionResult> Export(string period, CancellationToken ct)
    {
        var vm = new BillingIndexViewModel();
        await vm.PrepareData(_repos, _context, period, ct);
        return File(Encoding.UTF8.GetBytes(vm.ToCsv()), "text/csv", $"rtm-billing-{vm.Period.Key}.csv");
    }
}
