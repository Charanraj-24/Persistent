using Reflect.Web.Services.Audit;
using Reflect.Domain.Entities.ProviderDashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

namespace Reflect.Web.Areas.Provider.Controllers;

[Area("Provider")]
[Authorize(Policy = "ProviderOnly")]
public class ClientsController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly IProviderProfileRepository _providers;
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IMoodCheckInRepository _checkIns;
    private readonly IAssessmentResultRepository _assessments;
    private readonly IObservationRepository _observations;
    private readonly IRtmTimeEntryRepository _timeEntries;
    private readonly IAccessAuditRepository _audit;
    private readonly IClientNoteRepository _notes;

    public ClientsController(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments,
        IObservationRepository observations, IRtmTimeEntryRepository timeEntries, IAccessAuditRepository audit, IClientNoteRepository notes)
    {
        _currentUser = currentUser;
        _providers = providers;
        _clients = clients;
        _sessions = sessions;
        _checkIns = checkIns;
        _assessments = assessments;
        _observations = observations;
        _timeEntries = timeEntries;
        _audit = audit;
        _notes = notes;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new ClientIndexViewModel();
        await vm.PrepareData(_currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        return View(vm);
    }

    [AuditAccess(AuditAction.ViewClient)]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var vm = new ClientDetailsViewModel();
        await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _timeEntries, ct);
        return View(vm);
    }

    /// <summary>Session prep: a focused brief to read before a session. Time on this page is tracked as review time.</summary>
    [AuditAccess(AuditAction.ViewSessionPrep)]
    public async Task<IActionResult> Prep(Guid id, CancellationToken ct)
    {
        var vm = new ClientPrepViewModel();
        await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _timeEntries, ct);
        var notes = new Reflect.Web.Models.ViewModels.ProviderDashboard.Notes.ClientNotesViewModel();
        await notes.PrepareData(id, _currentUser, _providers, _clients, _notes, ct);
        vm.RecentNotes = notes.Notes.Take(3).ToList();
        vm.NoteTotal = notes.Notes.Count;
        return View(vm);
    }

    /// <summary>AJAX: the RTM status strip, refreshed when new data arrives for this client.</summary>
    public async Task<IActionResult> Status(Guid id, CancellationToken ct)
    {
        var vm = new ClientDetailsViewModel();
        await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _timeEntries, ct);
        return PartialView("_StatusStrip", vm);
    }

    /// <summary>AJAX: "Latest from Reflect", refreshed when live data arrives.</summary>
    [HttpGet]
    public async Task<IActionResult> Activity(Guid id, CancellationToken ct)
    {
        var vm = new ClientActivityViewModel();
        await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _checkIns, _assessments, _observations, ct);
        return PartialView("_Activity", vm);
    }

    /// <summary>AJAX: one tab of the client page, loaded the first time the provider opens it.</summary>
    [AuditAccess(AuditAction.ViewClientTab, "name")]
    public async Task<IActionResult> Tab(Guid id, string name, CancellationToken ct)
    {
        switch (name)
        {
            case "overview":
            {
                var vm = new ClientOverviewTabViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _checkIns, _assessments, ct);
                return PartialView("_TabOverview", vm);
            }
            case "engagement":
            {
                var vm = new ClientEngagementTabViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _checkIns, ct);
                vm.NoteCounts = await Reflect.Web.Models.ViewModels.ProviderDashboard.Notes.ClientNotesViewModel.CountsBySubjectAsync(id, _notes, ct);
                return PartialView("_TabEngagement", vm);
            }
            case "response":
            {
                var vm = new ClientResponseTabViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _checkIns, _assessments, ct);
                vm.NoteCounts = await Reflect.Web.Models.ViewModels.ProviderDashboard.Notes.ClientNotesViewModel.CountsBySubjectAsync(id, _notes, ct);
                return PartialView("_TabResponse", vm);
            }
            case "art":
            {
                var vm = new ClientArtTabViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _checkIns, ct);
                vm.NoteCounts = await Reflect.Web.Models.ViewModels.ProviderDashboard.Notes.ClientNotesViewModel.CountsBySubjectAsync(id, _notes, ct);
                return PartialView("_TabArt", vm);
            }
            case "notes":
            {
                var vm = new Reflect.Web.Models.ViewModels.ProviderDashboard.Notes.ClientNotesViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _notes, ct);
                return PartialView("_TabNotes", vm);
            }
            case "physiologic":
            {
                var vm = new ClientPhysiologicTabViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _observations, ct);
                return PartialView("_TabPhysiologic", vm);
            }
            case "rtm":
            {
                var vm = new ClientRtmTabViewModel();
                await vm.PrepareData(id, _currentUser, _providers, _clients, _sessions, _timeEntries, ct);
                vm.AccessHistory = new Reflect.Web.Models.ViewModels.ProviderDashboard.Audit.ClientAccessHistoryViewModel();
                await vm.AccessHistory.PrepareData(id, _currentUser, _providers, _clients, _audit, ct);
                return PartialView("_TabRtm", vm);
            }
            default:
                return NotFound();
        }
    }
}
