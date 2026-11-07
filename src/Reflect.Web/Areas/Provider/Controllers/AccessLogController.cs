using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Audit;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>Facility Admin: HIPAA access audit trail. Shows actions and pseudonymous record references only.</summary>
[Area("Provider")]
[Authorize(Policy = "FacilityAdminOnly")]
public class AccessLogController : Controller
{
    private readonly IAccessAuditRepository _audit;

    public AccessLogController(IAccessAuditRepository audit) => _audit = audit;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new AccessLogViewModel();
        await vm.PrepareData(_audit, ct);
        return View(vm);
    }
}
