using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Reports;

public class MonthlyReportRowViewModel
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SortName { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string Payer { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string DevicePeriod { get; set; } = string.Empty;
    public string DeviceSupply { get; set; } = string.Empty;
    public int DataDays { get; set; }
    public int Minutes { get; set; }
    public int PendingMinutes { get; set; }
    public string LiveInteraction { get; set; } = string.Empty;
    public string Codes { get; set; } = string.Empty;
    public List<BillableCode> CodeList { get; set; } = new();
    public BadgeViewModel Status { get; set; }
    public string Issues { get; set; } = string.Empty;
    public string EstimatedAmount => RtmFeeSchedule.Estimate(CodeList).Formatted;
}
