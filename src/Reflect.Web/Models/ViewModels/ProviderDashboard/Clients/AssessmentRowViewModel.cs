using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class AssessmentRowViewModel
{
    public Guid Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public long DateSort { get; set; }
    public string Instrument { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public BadgeViewModel Severity { get; set; }
    public string Change { get; set; } = string.Empty;
    public bool SafetyEndorsed { get; set; }

    public static AssessmentRowViewModel FromEntity(AssessmentResult a, AssessmentResult previous)
    {
        var delta = previous == null ? (int?)null : a.TotalScore - previous.TotalScore;
        return new AssessmentRowViewModel
        {
            Id = a.Id,
            Date = a.CompletedAt.ToUsDateString(),
            DateSort = a.CompletedAt.ToUnixTimeSeconds(),
            Instrument = AssessmentScoring.Name(a.Instrument),
            Score = a.TotalScore,
            MaxScore = AssessmentScoring.MaxScore(a.Instrument),
            Severity = new BadgeViewModel(AssessmentScoring.SeverityColor(a.Instrument, a.TotalScore), AssessmentScoring.Severity(a.Instrument, a.TotalScore), "ri-pulse-line"),
            Change = delta.HasValue ? (delta.Value > 0 ? $"+{delta.Value}" : delta.Value.ToString(CultureInfo.InvariantCulture)) : "Baseline",
            SafetyEndorsed = a.SafetyItemScore > 0
        };
    }
}
