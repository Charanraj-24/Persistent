using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.Provider.Monitoring;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderDashboardPolicies.ProviderOnly)]
public class MonitoringController : Controller
{
    private readonly ProviderDashboardRepositories _repos;
    private readonly ProviderContext _context;

    public MonitoringController(ProviderDashboardRepositories repos, ProviderContext context)
    {
        _repos = repos;
        _context = context;
    }

    public async Task<IActionResult> Index(string period, CancellationToken ct)
    {
        var vm = new MonitoringIndexViewModel();
        await vm.PrepareData(_repos, _context, period, ct);
        return View(vm);
    }

    public async Task<IActionResult> Export(string period, CancellationToken ct)
    {
        var vm = new MonitoringIndexViewModel();
        await vm.PrepareData(_repos, _context, period, ct);
        return File(Encoding.UTF8.GetBytes(vm.ToCsv()), "text/csv", $"monitoring-time-{vm.Period.Key}.csv");
    }

    public async Task<IActionResult> Create(Guid? patientId, CancellationToken ct)
    {
        var vm = new MonitoringCreateViewModel();
        await vm.PrepareData(_repos, _context, patientId, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MonitoringCreateViewModel vm, CancellationToken ct)
    {
        var saveData = await vm.PrepareDataForSave(_repos, _context, ModelState, ct);
        if (saveData == null)
        {
            return View(vm);
        }
        await _repos.MonitoringTimeEntryRepo.AddAsync(saveData.Entry, ct);
        TempData[TempDataKeys.Success] = ProviderDashboardMessages.ManualTimeLogged(vm.Minutes);
        return vm.ReturnToPatient
            ? RedirectToAction("Details", "Patients", new { id = saveData.Entry.PatientId, tab = "monitoring" })
            : RedirectToAction(nameof(Index));
    }

    // ---- AJAX: auto-tracked monitoring time while a patient record is open ----

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(Guid patientId, CancellationToken ct)
    {
        var tracking = new MonitoringTrackingViewModel();
        var saveData = await tracking.PrepareStart(_repos, _context, patientId, ct);
        await _repos.MonitoringTimeEntryRepo.AddAsync(saveData.Entry, ct);
        return Json(tracking.Result);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Heartbeat(Guid entryId, int activeSeconds, bool close, CancellationToken ct)
    {
        var tracking = new MonitoringTrackingViewModel();
        var saveData = await tracking.PrepareHeartbeat(_repos, _context, entryId, activeSeconds, close, ct);
        await _repos.MonitoringTimeEntryRepo.UpdateAsync(saveData.Entry, ct);
        return Json(tracking.Result);
    }
}
