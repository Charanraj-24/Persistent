using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

/// <summary>Dashboard tab: caseload activity, time by experience, RTM readiness by data-day tier and billing status.</summary>
public class DashboardTrendsTabViewModel
{
    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries,
        CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        var days = Enumerable.Range(0, 30).Select(i => snap.Today.AddDays(-29 + i)).ToList();
        var recent = snap.Sessions.Where(s => EasternTime.ToDate(s.StartedAt) >= days[0]).ToList();
        var byDay = recent.GroupBy(s => EasternTime.ToDate(s.StartedAt)).ToDictionary(g => g.Key, g => g.ToList());
        int Count(DateOnly d, ReflectPlatform p) => byDay.TryGetValue(d, out var l) ? l.Count(s => s.Platform == p) : 0;

        // RTM readiness: where each active client stands on data days in their current 30-day period.
        var tiers = new[] { "0-1 days (not billable)", "2-15 days (98986)", "16+ days (98978)" };
        var tierCounts = new int[3];
        var statusCounts = new Dictionary<string, int> { ["Billable"] = 0, ["In progress"] = 0, ["Action needed"] = 0 };
        foreach (var c in snap.Clients.Where(c => c.Status == ClientRtmStatus.Active))
        {
            var e = snap.EvaluateRtm(c);
            tierCounts[e.DataDaysInPeriod >= RtmBillingRules.MinDataDaysFullTier ? 2 : e.DataDaysInPeriod >= RtmBillingRules.MinDataDaysShortTier ? 1 : 0]++;
            var key = e.Status switch { RtmBillingStatus.Billable => "Billable", RtmBillingStatus.Blocked => "Action needed", _ => "In progress" };
            statusCounts[key]++;
        }

        ChartDataJson = JsonSerializer.Serialize(new
        {
            activity = new
            {
                labels = days.Select(d => d.ToString("M/d", CultureInfo.InvariantCulture)),
                xr = days.Select(d => Count(d, ReflectPlatform.ReflectXR)),
                mobile = days.Select(d => Count(d, ReflectPlatform.ReflectMobile))
            },
            experiences = recent.GroupBy(s => ModuleCatalog.Get(s.ModuleKey).GroupLabel)
                .Select(g => new { label = g.Key, minutes = Math.Round(g.Sum(s => s.DurationMinutes)) })
                .OrderByDescending(x => x.minutes),
            tiers = new { labels = tiers, counts = tierCounts },
            status = new { labels = statusCounts.Keys, counts = statusCounts.Values }
        });
    }
}
