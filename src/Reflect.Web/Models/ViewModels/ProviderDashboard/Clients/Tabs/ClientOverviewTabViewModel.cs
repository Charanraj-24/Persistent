using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

/// <summary>Overview tab: talking points, clinical flags, 30-day KPIs, daily time chart and client facts.</summary>
public class ClientOverviewTabViewModel
{
    public List<string> TalkingPoints { get; set; } = new();
    public List<ClinicalFlag> Flags { get; set; } = new();
    public EngagementSummary Summary { get; set; }
    public string TopModuleName { get; set; } = string.Empty;

    public string DateOfBirth { get; set; } = string.Empty;
    public string Payer { get; set; } = string.Empty;
    public string ConsentDate { get; set; } = string.Empty;
    public string RtmStartDate { get; set; } = string.Empty;
    public string Goals { get; set; } = string.Empty;

    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessionsRepo, IMoodCheckInRepository checkInsRepo,
        IAssessmentResultRepository assessmentsRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var from = scope.DaysAgoUtc(CaseloadSnapshot.LookbackDays);
        var sessions = await sessionsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var checkIns = await checkInsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var assessments = await assessmentsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);

        var last30 = sessions.Where(s => s.StartedAt > scope.NowUtc.AddDays(-30)).ToList();
        var checkIns30 = checkIns.Where(c => c.RecordedAt > scope.NowUtc.AddDays(-30)).ToList();

        Summary = EngagementAnalyzer.Summarize(last30, checkIns30, scope.NowUtc, EasternTime.ToDate);
        TopModuleName = string.IsNullOrEmpty(Summary.TopModuleKey) ? "None" : ModuleCatalog.Get(Summary.TopModuleKey).Name;
        var full = EngagementAnalyzer.Summarize(sessions, checkIns30, scope.NowUtc, EasternTime.ToDate);
        Flags = EngagementAnalyzer.Flags(full, assessments, scope.NowUtc);
        TalkingPoints = TalkingPointsBuilder.Build(sessions, checkIns, assessments, scope.NowUtc);

        var c = scope.Client;
        DateOfBirth = c.DateOfBirth.ToUsDateString();
        Payer = c.PayerName;
        ConsentDate = c.RtmConsentDate.HasValue ? c.RtmConsentDate.Value.ToUsDateString() : "Not on file";
        RtmStartDate = c.RtmStartDate.ToUsDateString();
        Goals = c.TreatmentGoals;

        var days = Enumerable.Range(0, 30).Select(i => scope.Today.AddDays(-29 + i)).ToList();
        var byDay = last30.GroupBy(s => EasternTime.ToDate(s.StartedAt)).ToDictionary(g => g.Key, g => g.ToList());
        double Minutes(DateOnly d, ReflectPlatform p) =>
            byDay.TryGetValue(d, out var l) ? Math.Round(l.Where(s => s.Platform == p).Sum(s => s.DurationMinutes)) : 0;

        ChartDataJson = JsonSerializer.Serialize(new
        {
            daily = new
            {
                labels = days.Select(d => d.ToString("M/d", CultureInfo.InvariantCulture)),
                xr = days.Select(d => Minutes(d, ReflectPlatform.ReflectXR)),
                mobile = days.Select(d => Minutes(d, ReflectPlatform.ReflectMobile))
            },
            platform = new { xr = Math.Round(Summary.XrMinutes), mobile = Math.Round(Summary.MobileMinutes) }
        });
    }
}
