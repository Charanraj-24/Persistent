using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Catalogs;

public sealed record ReflectModule(string Key, string Name, ReflectPlatform Platform, string Description, string Icon);

/// <summary>The Reflect XR experiences and Reflect Mobile modules the dashboard reports on.</summary>
public static class ModuleCatalog
{
    public const string AppreciationForLife = "Appreciation for Life";
    public const string NewPathways = "New Pathways";

    public static readonly IReadOnlyList<ReflectModule> All = new List<ReflectModule>
    {
        new("xr-creative-calm", "Creative Calm", ReflectPlatform.ReflectXR, "Guided, calming art-making in VR.", "ri-leaf-line"),
        new("xr-explore-emotions", "Explore Emotions", ReflectPlatform.ReflectXR, "Name, shape, and color emotions in VR.", "ri-emotion-line"),
        new("xr-explore-self", "Explore Self", ReflectPlatform.ReflectXR, "Self-portrait and identity reflection in VR.", "ri-user-heart-line"),
        new("xr-explore-journey", "Explore Journey", ReflectPlatform.ReflectXR, "Pathway-based journeys: New Pathways and Appreciation for Life.", "ri-route-line"),
        new("mobile-learn", "Learn", ReflectPlatform.ReflectMobile, "Short lessons on art-therapy concepts.", "ri-book-open-line"),
        new("mobile-create", "Create", ReflectPlatform.ReflectMobile, "AI-generated imagery from prompts.", "ri-palette-line"),
        new("mobile-reflect", "Reflect", ReflectPlatform.ReflectMobile, "Written reflections on creations.", "ri-quill-pen-line"),
        new("mobile-explore-emotions", "Explore Emotions", ReflectPlatform.ReflectMobile, "Emotion check-ins with imagery.", "ri-emotion-happy-line")
    };

    private static readonly Dictionary<string, ReflectModule> ByKey = All.ToDictionary(m => m.Key);

    public static ReflectModule Get(string key) =>
        ByKey.TryGetValue(key, out var module) ? module : new ReflectModule(key, key, ReflectPlatform.ReflectMobile, string.Empty, "ri-apps-line");

    public static IEnumerable<ReflectModule> ForPlatform(ReflectPlatform platform) => All.Where(m => m.Platform == platform);

    public static string PlatformName(ReflectPlatform platform) => platform == ReflectPlatform.ReflectXR ? "Reflect XR" : "Reflect Mobile";
}
