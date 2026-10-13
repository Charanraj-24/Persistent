using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.Provider.Communications;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderDashboardPolicies.ProviderOnly)]
public class CommunicationsController : Controller
{
    private readonly ProviderDashboardRepositories _repos;
    private readonly ProviderContext _context;

    public CommunicationsController(ProviderDashboardRepositories repos, ProviderContext context)
    {
        _repos = repos;
        _context = context;
    }

    public async Task<IActionResult> Index(string period, CancellationToken ct)
    {
        var vm = new CommunicationIndexViewModel();
        await vm.PrepareData(_repos, _context, period, ct);
        return View(vm);
    }

    public async Task<IActionResult> Create(Guid? patientId, CancellationToken ct)
    {
        var vm = new CommunicationCreateViewModel();
        await vm.PrepareData(_repos, _context, patientId, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommunicationCreateViewModel vm, CancellationToken ct)
    {
        var saveData = await vm.PrepareDataForSave(_repos, _context, ModelState, ct);
        if (saveData == null)
        {
            return View(vm);
        }
        await _repos.CareCommunicationRepo.AddAsync(saveData.Communication, ct);
        TempData[TempDataKeys.Success] = ProviderDashboardMessages.CommunicationLogged;
        return vm.ReturnToPatient
            ? RedirectToAction("Details", "Patients", new { id = saveData.Communication.PatientId, tab = "communications" })
            : RedirectToAction(nameof(Index));
    }
}
