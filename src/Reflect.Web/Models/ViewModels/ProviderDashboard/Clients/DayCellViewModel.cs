using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class DayCellViewModel
{
    public string Label { get; set; } = string.Empty;
    public int Day { get; set; }
    public bool HasData { get; set; }
    public bool IsFuture { get; set; }
    public bool IsToday { get; set; }
}
