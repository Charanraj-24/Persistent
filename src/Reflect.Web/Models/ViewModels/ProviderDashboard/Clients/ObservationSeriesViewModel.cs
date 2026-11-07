using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class ObservationSeriesViewModel
{
    public string Code { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int Readings { get; set; }
    public string Latest { get; set; } = string.Empty;
    public string LatestAt { get; set; } = string.Empty;
    public string AveragePre { get; set; } = string.Empty;
    public string AveragePost { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
