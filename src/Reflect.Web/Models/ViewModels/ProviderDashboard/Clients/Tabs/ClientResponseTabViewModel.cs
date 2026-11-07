using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

/// <summary>Therapeutic response tab: mood and stress before/after sessions, PHQ-9/GAD-7 trend, emotion words, history table.</summary>
public class ClientResponseTabViewModel
{
    public List<AssessmentRowViewModel> Assessments { get; set; } = new();
    public List<string> TopEmotions { get; set; } = new();
    public int PairedCheckIns { get; set; }
    public string LatestPhq { get; set; } = string.Empty;
    public string LatestGad { get; set; } = string.Empty;
    public BadgeViewModel LatestPhqBadge { get; set; }
    public BadgeViewModel LatestGadBadge { get; set; }
    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IMoodCheckInRepository checkInsRepo, IAssessmentResultRepository assessmentsRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var checkIns = await checkInsRepo.GetForClientsAsync(scope.UserIds, scope.DaysAgoUtc(30), scope.NowUtc, ct);
        var assessments = await assessmentsRepo.GetForClientsAsync(scope.UserIds, scope.DaysAgoUtc(CaseloadSnapshot.LookbackDays), scope.NowUtc, ct);

        TopEmotions = checkIns.Where(c => !string.IsNullOrWhiteSpace(c.EmotionTags))
            .SelectMany(c => c.EmotionTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .Select(g => $"{g.Key} ({g.Count()})")
            .ToList();

        AssessmentResult prevPhq = null, prevGad = null;
        foreach (var a in assessments.OrderBy(a => a.CompletedAt))
        {
            var prev = a.Instrument == AssessmentInstrument.Phq9 ? prevPhq : prevGad;
            Assessments.Add(AssessmentRowViewModel.FromEntity(a, prev));
            if (a.Instrument == AssessmentInstrument.Phq9) prevPhq = a; else prevGad = a;
        }
        Assessments.Reverse();
        var lp = Assessments.FirstOrDefault(a => a.Instrument == "PHQ-9");
        var lg = Assessments.FirstOrDefault(a => a.Instrument == "GAD-7");
        LatestPhq = lp == null ? "-" : $"{lp.Score}";
        LatestGad = lg == null ? "-" : $"{lg.Score}";
        LatestPhqBadge = lp?.Severity;
        LatestGadBadge = lg?.Severity;

        var pairs = checkIns.Where(c => c.AppSessionId.HasValue).GroupBy(c => c.AppSessionId.Value)
            .Select(g => new { Pre = g.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession), Post = g.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession) })
            .Where(p => p.Pre != null && p.Post != null)
            .OrderBy(p => p.Pre.RecordedAt)
            .ToList();
        PairedCheckIns = pairs.Count;

        var phq = assessments.Where(a => a.Instrument == AssessmentInstrument.Phq9).OrderBy(a => a.CompletedAt).ToList();
        var gad = assessments.Where(a => a.Instrument == AssessmentInstrument.Gad7).OrderBy(a => a.CompletedAt).ToList();
        string Day(DateTimeOffset t) => t.ToEastern().ToString("M/d", CultureInfo.InvariantCulture);

        ChartDataJson = JsonSerializer.Serialize(new
        {
            checkIns = new
            {
                labels = pairs.Select(p => Day(p.Pre.RecordedAt)),
                moodPre = pairs.Select(p => p.Pre.MoodScore),
                moodPost = pairs.Select(p => p.Post.MoodScore),
                stressPre = pairs.Select(p => p.Pre.StressScore),
                stressPost = pairs.Select(p => p.Post.StressScore)
            },
            assessments = new { labels = phq.Select(a => Day(a.CompletedAt)), phq = phq.Select(a => a.TotalScore), gad = gad.Select(a => a.TotalScore) }
        });
    }
}
