using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Patients;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderDashboardPolicies.ProviderOnly)]
public class PatientsController : Controller
{
    private readonly ProviderDashboardRepositories _repos;
    private readonly ProviderContext _context;

    public PatientsController(ProviderDashboardRepositories repos, ProviderContext context)
    {
        _repos = repos;
        _context = context;
    }

    public async Task<IActionResult> Index(string filter, CancellationToken ct)
    {
        var vm = new PatientIndexViewModel();
        await vm.PrepareData(_repos, _context, filter, ct);
        return View(vm);
    }

    public async Task<IActionResult> Details(Guid id, string tab, CancellationToken ct)
    {
        var vm = new PatientDetailsViewModel();
        await vm.PrepareData(_repos, _context, id, ct, tab);
        return View(vm);
    }
}
