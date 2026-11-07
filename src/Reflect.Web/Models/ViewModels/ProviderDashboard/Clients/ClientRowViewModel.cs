using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class ClientRowViewModel
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SortName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string DiagnosisLabel { get; set; } = string.Empty;
    public string LastActivity { get; set; } = string.Empty;
    public long LastActivitySort { get; set; }
    public int DataDays { get; set; }
    public int DataDaysPercent { get; set; }
    public int Minutes { get; set; }
    public int MinutesPercent { get; set; }
    public int PendingMinutes { get; set; }
    public bool HasLiveInteraction { get; set; }
    public string Codes { get; set; } = string.Empty;
    public BadgeViewModel Status { get; set; }
    public string NextStep { get; set; } = string.Empty;
    public ClinicalFlag TopFlag { get; set; }
    public int FlagSort { get; set; }
    public bool IsPaused { get; set; }

    public static ClientRowViewModel FromEntity(ProviderClient c, RtmEvaluation eval, EngagementSummary summary, List<ClinicalFlag> flags)
    {
        var top = flags.FirstOrDefault(f => f.Severity is FlagSeverity.Critical or FlagSeverity.Warning);
        return new ClientRowViewModel
        {
            ClientId = c.Id,
            Name = c.FullName,
            SortName = $"{c.LastName}, {c.FirstName}",
            Initials = c.Initials,
            Mrn = c.Mrn,
            DiagnosisCode = c.PrimaryDiagnosisCode,
            DiagnosisLabel = c.PrimaryDiagnosisLabel,
            LastActivity = summary.LastSessionAt.HasValue ? summary.LastSessionAt.Value.ToUsDateString() : "None in 30 days",
            LastActivitySort = summary.LastSessionAt?.ToUnixTimeSeconds() ?? 0,
            DataDays = eval.DataDaysInPeriod,
            DataDaysPercent = RtmStatusPresenter.DataDaysPercent(eval),
            Minutes = eval.ManagementMinutes,
            MinutesPercent = RtmStatusPresenter.MinutesPercent(eval),
            PendingMinutes = eval.PendingMinutes,
            HasLiveInteraction = eval.HasRealTimeInteraction,
            Codes = RtmStatusPresenter.CodesText(eval),
            Status = c.Status == ClientRtmStatus.Paused ? new BadgeViewModel("info", "Paused", "ri-pause-circle-line") : RtmStatusPresenter.StatusBadge(eval),
            NextStep = c.Status == ClientRtmStatus.Paused ? "Monitoring paused." : RtmStatusPresenter.NextStep(eval),
            TopFlag = top,
            FlagSort = top == null ? 9 : (int)top.Severity,
            IsPaused = c.Status == ClientRtmStatus.Paused
        };
    }
}
