using Reflect.Web.Services.Audit;
using Reflect.Domain.Entities.ProviderDashboard;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.FacilityBilling;

namespace Reflect.Web.Areas.Provider.Controllers;

/// <summary>
/// Facility Admin: RTM billing totals by provider. Separate policy from the clinical screens; admins never see
/// client names or clinical data, and providers can't open this screen.
/// </summary>
[Area("Provider")]
[Authorize(Policy = "FacilityAdminOnly")]
public class FacilityBillingController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IFacilityAccessService _facilityAccess;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IRtmTimeEntryRepository _timeEntries;

    public FacilityBillingController(ICurrentUserService currentUser, IFacilityAccessService facilityAccess, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessions, IRtmTimeEntryRepository timeEntries)
    {
        _currentUser = currentUser;
        _facilityAccess = facilityAccess;
        _providers = providers;
        _clients = clients;
        _sessions = sessions;
        _timeEntries = timeEntries;
    }

    [AuditAccess(AuditAction.ViewBilling, "month")]
    public async Task<IActionResult> Index(string month, Guid? facilityId, CancellationToken ct)
    {
        var vm = new FacilityBillingViewModel();
        await vm.PrepareData(month, facilityId, _currentUser, _facilityAccess, _providers, _clients, _sessions, _timeEntries, ct);
        return View(vm);
    }

    [AuditAccess(AuditAction.ExportReport, "month")]
    public async Task<IActionResult> ExportCsv(string month, Guid? facilityId, CancellationToken ct)
    {
        var vm = new FacilityBillingViewModel();
        await vm.PrepareData(month, facilityId, _currentUser, _facilityAccess, _providers, _clients, _sessions, _timeEntries, ct);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(vm.ToCsv())).ToArray();
        return File(bytes, "text/csv", $"rtm-by-provider-{vm.Month}.csv");
    }
}
