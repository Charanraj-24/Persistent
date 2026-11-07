using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

/// <summary>Physiologic tab (RPM-ready): any observation codes present, heart rate around sessions.</summary>
public class ClientPhysiologicTabViewModel
{
    public List<ObservationSeriesViewModel> Series { get; set; } = new();
    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IObservationRepository observationsRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var observations = await observationsRepo.GetForClientAsync(scope.Client.ClientUserId, scope.DaysAgoUtc(30), scope.NowUtc, ct);

        Series = observations.GroupBy(o => o.Code).Select(g =>
        {
            var info = ObservationCatalog.Get(g.Key);
            var pre = g.Where(o => o.Context == "pre").ToList();
            var post = g.Where(o => o.Context == "post").ToList();
            var latest = g.OrderByDescending(o => o.EffectiveAt).First();
            return new ObservationSeriesViewModel
            {
                Code = g.Key,
                Display = info.Display,
                Unit = info.Unit,
                Readings = g.Count(),
                Latest = latest.Value.ToString("0", CultureInfo.InvariantCulture),
                LatestAt = latest.EffectiveAt.ToUsDateTimeString(),
                AveragePre = pre.Count == 0 ? "-" : pre.Average(o => o.Value).ToString("0", CultureInfo.InvariantCulture),
                AveragePost = post.Count == 0 ? "-" : post.Average(o => o.Value).ToString("0", CultureInfo.InvariantCulture),
                Source = latest.SourceDevice
            };
        }).ToList();

        var hr = observations.Where(o => o.Code == ObservationCatalog.HeartRate && o.AppSessionId.HasValue)
            .GroupBy(o => o.AppSessionId.Value)
            .Select(g => new { Pre = g.FirstOrDefault(o => o.Context == "pre"), Post = g.FirstOrDefault(o => o.Context == "post") })
            .Where(p => p.Pre != null && p.Post != null)
            .OrderBy(p => p.Pre.EffectiveAt)
            .ToList();

        ChartDataJson = JsonSerializer.Serialize(new
        {
            heartRate = new
            {
                labels = hr.Select(p => p.Pre.EffectiveAt.ToEastern().ToString("M/d", CultureInfo.InvariantCulture)),
                pre = hr.Select(p => p.Pre.Value),
                post = hr.Select(p => p.Post.Value)
            }
        });
    }
}
