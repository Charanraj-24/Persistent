using Reflect.Web.Services.Audit;
using Reflect.Domain.Entities.ProviderDashboard;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Reports;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class ReportsController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IMoodCheckInRepository _checkIns;
    private readonly IAssessmentResultRepository _assessments;
    private readonly IRtmTimeEntryRepository _timeEntries;

    public ReportsController(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
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

    public async Task<IActionResult> Monthly(string month, CancellationToken ct)
    {
        var vm = new MonthlyReportViewModel();
        await vm.PrepareData(month, _currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        return View(vm);
    }

    [AuditAccess(AuditAction.ExportReport, "month")]
    public async Task<IActionResult> ExportCsv(string month, CancellationToken ct)
    {
        var vm = new MonthlyReportViewModel();
        await vm.PrepareData(month, _currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(vm.ToCsv())).ToArray();
        return File(bytes, "text/csv", $"rtm-summary-{vm.Month}.csv");
    }

    [AuditAccess(AuditAction.PrintRtmDocumentation, "month")]
    public async Task<IActionResult> ClientSummary(Guid id, string month, CancellationToken ct)
    {
        var vm = new ClientSummaryViewModel();
        await vm.PrepareData(id, month, _currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        return View(vm);
    }
}
