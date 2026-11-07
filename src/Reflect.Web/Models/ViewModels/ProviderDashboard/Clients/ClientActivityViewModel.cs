using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

/// <summary>
/// "Latest from Reflect" on the client page: the newest sessions, questionnaires and readings, whatever their source,
/// with anything that arrived live in the last 30 minutes marked New. Refreshed when live data arrives.
/// </summary>
public class ClientActivityViewModel
{
    public const int MaxItems = 5;
    public static readonly TimeSpan NewWindow = TimeSpan.FromMinutes(30);

    public List<ClientActivityItemViewModel> Items { get; set; } = new();
    public int NewCount { get; set; }

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clients,
        IAppSessionRepository sessionsRepo, IMoodCheckInRepository checkInsRepo, IAssessmentResultRepository assessmentsRepo,
        IObservationRepository observationsRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var now = scope.NowUtc;
        var from = scope.DaysAgoUtc(30);
        var sessions = await sessionsRepo.GetForClientsAsync(scope.UserIds, from, now.AddMinutes(5), ct);
        var checkIns = await checkInsRepo.GetForClientsAsync(scope.UserIds, from, now.AddMinutes(5), ct);
        var assessments = await assessmentsRepo.GetForClientsAsync(scope.UserIds, from, now.AddMinutes(5), ct);
        var observations = await observationsRepo.GetForClientAsync(scope.Client.ClientUserId, scope.DaysAgoUtc(7), now.AddMinutes(5), ct);
        var bySession = checkIns.Where(c => c.AppSessionId.HasValue).GroupBy(c => c.AppSessionId!.Value).ToDictionary(g => g.Key, g => g.ToList());

        bool IsNew(string source, DateTimeOffset created) => source.Length > 0 && now - created <= NewWindow;

        var items = new List<ClientActivityItemViewModel>();
        foreach (var s in sessions)
        {
            var m = ModuleCatalog.Get(s.ModuleKey);
            var list = bySession.TryGetValue(s.Id, out var l) ? l : new List<MoodCheckIn>();
            var pre = list.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession);
            var post = list.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession);
            var mood = pre != null && post != null ? $", mood {pre.MoodScore} to {post.MoodScore}" : string.Empty;
            var words = post?.EmotionTags ?? string.Empty;
            items.Add(new ClientActivityItemViewModel
            {
                At = s.EndedAt, Key = "s" + s.Id.ToString("N"),
                Title = $"{(s.Completed ? "Finished" : "Started")} {m.Name}",
                Detail = $"{m.PlatformLabel}, {m.Group}, {Math.Round(s.DurationMinutes)} min{mood}{(words.Length > 0 ? ", felt " + words.Replace(",", ", ") : string.Empty)}",
                Icon = s.Platform == ReflectPlatform.ReflectXR ? "ri-goggles-line" : "ri-smartphone-line",
                SourceLabel = Label(s.Source), IsNew = IsNew(s.Source, s.DateCreated)
            });
        }
        foreach (var a in assessments)
        {
            var name = a.Instrument == AssessmentInstrument.Phq9 ? "PHQ-9" : "GAD-7";
            var urgent = a.Instrument == AssessmentInstrument.Phq9 && a.SafetyItemScore > 0;
            items.Add(new ClientActivityItemViewModel
            {
                At = a.CompletedAt, Key = "a" + a.Id.ToString("N"),
                Title = $"Completed {name}: {a.TotalScore}",
                Detail = AssessmentScoring.Severity(a.Instrument, a.TotalScore) + (urgent ? ". Item 9 (self-harm) endorsed" : string.Empty),
                Icon = urgent ? "ri-alarm-warning-line" : "ri-survey-line", IsUrgent = urgent,
                SourceLabel = Label(a.Source), IsNew = IsNew(a.Source, a.DateCreated)
            });
        }
        foreach (var o in observations)
        {
            var t = ObservationCatalog.Get(o.Code);
            items.Add(new ClientActivityItemViewModel
            {
                At = o.EffectiveAt, Key = "o" + o.Id.ToString("N"),
                Title = $"{t.Display}: {o.Value:0.#} {o.Unit}", Detail = string.IsNullOrEmpty(o.Context) ? "Wearable reading (RPM preview)" : o.Context + " (RPM preview)",
                Icon = "ri-heart-pulse-line", SourceLabel = Label(o.Source), IsNew = IsNew(o.Source, o.DateCreated)
            });
        }

        Items = items.OrderByDescending(i => i.At).Take(MaxItems).ToList();
        foreach (var i in Items) i.When = Relative(i.At, now);
        NewCount = Items.Count(i => i.IsNew);
    }

    private static string Label(string source) => source switch
    {
        "phone" => "Audience phone",
        "api" => "Reflect app",
        "live demo" => "Live demo",
        "simulator" => "Simulator",
        _ => string.Empty
    };

    private static string Relative(DateTimeOffset at, DateTimeOffset now)
    {
        var ago = now - at;
        if (ago < TimeSpan.FromMinutes(1)) return "Just now";
        if (ago < TimeSpan.FromHours(1)) return $"{(int)ago.TotalMinutes} min ago";
        var day = EasternTime.ToDate(at);
        var today = EasternTime.ToDate(now);
        var time = EasternTime.TimeOfDay(at).ToString("h:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        if (day == today) return "Today " + time;
        if (day == today.AddDays(-1)) return "Yesterday " + time;
        return at.ToUsDateString();
    }
}
