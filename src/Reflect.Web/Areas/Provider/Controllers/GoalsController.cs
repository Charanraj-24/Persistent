using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.Provider.Goals;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = ProviderDashboardPolicies.ProviderOnly)]
public class GoalsController : Controller
{
    private readonly ProviderDashboardRepositories _repos;
    private readonly ProviderContext _context;

    public GoalsController(ProviderDashboardRepositories repos, ProviderContext context)
    {
        _repos = repos;
        _context = context;
    }

    public async Task<IActionResult> Create(Guid patientId, CancellationToken ct)
    {
        var vm = new GoalFormViewModel();
        await vm.PrepareDataForCreate(_repos, _context, patientId, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GoalFormViewModel vm, CancellationToken ct)
    {
        var saveData = await vm.PrepareDataForSave(_repos, _context, ModelState, ct);
        if (saveData == null)
        {
            return View(vm);
        }
        await _repos.TreatmentGoalRepo.AddAsync(saveData.Goal, ct);
        TempData[TempDataKeys.Success] = ProviderDashboardMessages.GoalCreated;
        return RedirectToAction("Details", "Patients", new { id = saveData.Goal.PatientId, tab = "goals" });
    }

    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var vm = new GoalFormViewModel();
        await vm.PrepareDataForEdit(_repos, _context, id, ct);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(GoalFormViewModel vm, CancellationToken ct)
    {
        var saveData = await vm.PrepareDataForSave(_repos, _context, ModelState, ct);
        if (saveData == null)
        {
            return View(vm);
        }
        await _repos.TreatmentGoalRepo.UpdateAsync(saveData.Goal, ct);
        TempData[TempDataKeys.Success] = ProviderDashboardMessages.GoalUpdated;
        return RedirectToAction("Details", "Patients", new { id = saveData.Goal.PatientId, tab = "goals" });
    }
}
