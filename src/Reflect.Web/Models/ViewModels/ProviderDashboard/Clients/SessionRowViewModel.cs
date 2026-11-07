using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class SessionRowViewModel
{
    public Guid Id { get; set; }
    public string When { get; set; } = string.Empty;
    public long WhenSort { get; set; }
    public string PlatformLabel { get; set; } = string.Empty;
    public string PlatformIcon { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public bool Completed { get; set; }
    public string MoodChange { get; set; } = string.Empty;
    public int MoodChangeSort { get; set; }
    public string Emotions { get; set; } = string.Empty;

    public static SessionRowViewModel FromEntity(AppSession s, List<MoodCheckIn> checkIns)
    {
        var pre = checkIns.FirstOrDefault(c => c.Phase == CheckInPhase.PreSession);
        var post = checkIns.FirstOrDefault(c => c.Phase == CheckInPhase.PostSession);
        var change = pre != null && post != null ? post.MoodScore - pre.MoodScore : (int?)null;
        return new SessionRowViewModel
        {
            Id = s.Id,
            When = s.StartedAt.ToUsDateTimeString(),
            WhenSort = s.StartedAt.ToUnixTimeSeconds(),
            PlatformLabel = s.Platform == ReflectPlatform.ReflectXR ? "Reflect XR" : "Reflect Mobile",
            PlatformIcon = s.Platform == ReflectPlatform.ReflectXR ? "ri-goggles-line" : "ri-smartphone-line",
            ModuleName = ModuleCatalog.Get(s.ModuleKey).Name,
            Minutes = (int)Math.Round(s.DurationMinutes),
            Completed = s.Completed,
            MoodChange = change.HasValue ? $"{pre.MoodScore} to {post.MoodScore} ({(change.Value >= 0 ? "+" : "")}{change.Value})" : "Not recorded",
            MoodChangeSort = change ?? -99,
            Emotions = post?.EmotionTags.Replace(",", ", ") ?? string.Empty
        };
    }
}
