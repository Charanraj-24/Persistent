using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;
using Reflect.Web.Services.Art;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

/// <summary>Artwork tab: a visual timeline of the pieces the client made in Reflect, with mood and emotion words from each session.</summary>
public class ClientArtTabViewModel
{
    public List<ArtworkTileViewModel> Tiles { get; set; } = new();
    public int TotalPieces { get; set; }
    public List<(string Word, int Count)> TopWords { get; set; } = new();

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessionsRepo, IMoodCheckInRepository checkInsRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var from = scope.DaysAgoUtc(60);
        var sessions = (await sessionsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct)).Where(s => s.ArtworksCreated > 0).ToList();
        var checkIns = await checkInsRepo.GetForClientsAsync(scope.UserIds, from, scope.NowUtc, ct);
        var bySession = checkIns.Where(c => c.AppSessionId.HasValue).GroupBy(c => c.AppSessionId.Value).ToDictionary(g => g.Key, g => g.ToList());

        TotalPieces = sessions.Sum(s => s.ArtworksCreated);
        foreach (var s in sessions.OrderByDescending(s => s.StartedAt).Take(24))
        {
            var list = bySession.TryGetValue(s.Id, out var l) ? l : new List<MoodCheckIn>();
            var pre = list.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession);
            var post = list.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession);
            var tags = (post?.EmotionTags ?? pre?.EmotionTags ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            Tiles.Add(new ArtworkTileViewModel
            {
                Svg = SyntheticArtworkRenderer.Render(s.Id, tags, "art" + s.Id.ToString("N")[..8]),
                Module = ModuleCatalog.Get(s.ModuleKey).Name.Replace(" (Mobile)", string.Empty),
                IsXr = s.Platform == ReflectPlatform.ReflectXR,
                Date = s.StartedAt.ToUsDateString(),
                Mood = pre != null && post != null ? $"{pre.MoodScore} to {post.MoodScore}" : post != null ? post.MoodScore.ToString() : null,
                MoodLift = pre != null && post != null ? post.MoodScore - pre.MoodScore : 0,
                Words = tags,
                Pieces = s.ArtworksCreated
            });
        }
        TopWords = Tiles.SelectMany(t => t.Words).GroupBy(w => w).Select(g => (g.Key, g.Count())).OrderByDescending(x => x.Item2).Take(6).ToList();
    }
}
