using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class CompletedPeriodViewModel
{
    public string Label { get; set; } = string.Empty;
    public int DataDays { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static CompletedPeriodViewModel FromDomain(CompletedDevicePeriod p) => new()
    {
        Label = $"{p.Period.Start.ToUsDateString()} to {p.Period.End.ToUsDateString()}",
        DataDays = p.DataDays,
        Code = p.Code,
        Description = string.IsNullOrEmpty(p.Code) ? "Under 2 data days, not billable" : RtmBillingRules.Describe(p.Code)
    };
}
