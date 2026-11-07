using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

/// <summary>
/// Dashboard tab: which clients use which Reflect modules, and how much. Ships one compact dataset
/// (minutes and sessions per client per module for 7, 30 and 90 days) so filtering and sorting in the browser are instant.
/// The tab reloads itself when live data arrives, and the browser keeps the chosen filters.
/// </summary>
public class DashboardUsageTabViewModel
{
    public static readonly int[] Windows = { 7, 30, 90 };

    public string DataJson { get; set; } = "{}";
    public int ClientCount { get; set; }
    public int ModulesUsed { get; set; }
    public string AsOf { get; set; } = string.Empty;

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessions, IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries,
        CancellationToken ct)
    {
        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, null, ct);
        var list = snap.Clients.Where(c => c.Status != ClientRtmStatus.Discharged).OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToList();
        var modules = ModuleCatalog.All.ToList();
        var moduleIndex = modules.Select((m, i) => (m.Key, i)).ToDictionary(x => x.Key, x => x.i);
        var now = snap.NowUtc;

        var usage = new List<object>();
        var used = new HashSet<int>();
        for (var ci = 0; ci < list.Count; ci++)
        {
            var own = snap.SessionsFor(list[ci]).Where(s => s.StartedAt > now.AddDays(-Windows[^1]) && s.StartedAt <= now).ToList();
            foreach (var g in own.GroupBy(s => s.ModuleKey))
            {
                if (!moduleIndex.TryGetValue(g.Key, out var mi)) continue;
                used.Add(mi);
                usage.Add(new
                {
                    c = ci,
                    m = mi,
                    min = Windows.Select(w => (int)Math.Round(g.Where(s => s.StartedAt > now.AddDays(-w)).Sum(s => s.DurationMinutes))).ToArray(),
                    ses = Windows.Select(w => g.Count(s => s.StartedAt > now.AddDays(-w))).ToArray(),
                    last = g.Max(s => s.StartedAt).ToUnixTimeMilliseconds()
                });
            }
        }

        ClientCount = list.Count;
        ModulesUsed = used.Count;
        AsOf = Reflect.Domain.Helpers.DateTimeOffsetUsExtensions.ToUsDateTimeString(now);
        DataJson = JsonSerializer.Serialize(new
        {
            windows = Windows,
            now = now.ToUnixTimeMilliseconds(),
            clients = list.Select(c => new { id = c.Id, name = c.FullName, initials = $"{c.FirstName[..1]}{c.LastName[..1]}" }),
            modules = modules.Select(m => new
            {
                key = m.Key, name = m.Name, group = m.Group, groupLabel = m.GroupLabel,
                platform = m.Platform == ReflectPlatform.ReflectXR ? "xr" : "mobile", ai = m.UsesAi, tutorial = m.IsTutorial
            }),
            usage
        });
    }
}
