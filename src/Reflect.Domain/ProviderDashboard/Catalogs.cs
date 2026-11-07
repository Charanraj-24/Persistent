using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Domain.ProviderDashboard;

public sealed record ModuleInfo(string Key, string Name, ReflectPlatform Platform, string Group, bool UsesAi = false, bool IsTutorial = false)
{
    public string PlatformLabel => Platform == ReflectPlatform.ReflectXR ? "Reflect XR" : "Reflect Mobile";
    /// <summary>Group with platform, because "Creative Calm" exists in both apps.</summary>
    public string GroupLabel => $"{Group} ({(Platform == ReflectPlatform.ReflectXR ? "XR" : "Mobile")})";
}

/// <summary>
/// PTI's official module catalog ("Reflect Product Catalog: Official Modules &amp; Pricing", effective 9/9/2026).
/// Reflect Mobile: 14 modules in 4 groups. Reflect XR Home: 17 modules in 5 programs, including the Training Tutorial.
/// </summary>
public static class ModuleCatalog
{
    public const string TutorialKey = "xr-tutorial";

    public static readonly IReadOnlyList<ModuleInfo> All = new List<ModuleInfo>
    {
        // Reflect Mobile
        new("m-floating-bubbles", "Floating Bubbles", ReflectPlatform.ReflectMobile, "Creative Calm"),
        new("m-finger-painting", "Finger Painting", ReflectPlatform.ReflectMobile, "Creative Calm"),
        new("m-pour-paint", "Pour Paint", ReflectPlatform.ReflectMobile, "Creative Calm"),
        new("m-breathing-mandala", "Breathing Mandala", ReflectPlatform.ReflectMobile, "Creative Calm"),
        new("m-photo-coloring", "Photo Coloring", ReflectPlatform.ReflectMobile, "Creative Calm", UsesAi: true),
        new("m-ai-mandala", "AI Created Mandala", ReflectPlatform.ReflectMobile, "Creative Calm", UsesAi: true),
        new("m-create-own-prompt", "Create Your Own Prompt", ReflectPlatform.ReflectMobile, "Create", UsesAi: true),
        new("m-options-prompt", "Options Prompt Pathway", ReflectPlatform.ReflectMobile, "Create", UsesAi: true),
        new("m-select-images", "Select from Images Pathway", ReflectPlatform.ReflectMobile, "Create", UsesAi: true),
        new("m-learn-creativity", "Learn: Creativity", ReflectPlatform.ReflectMobile, "Learn", UsesAi: true),
        new("m-learn-emotions", "Learn: Emotions", ReflectPlatform.ReflectMobile, "Learn", UsesAi: true),
        new("m-learn-self", "Learn: Self", ReflectPlatform.ReflectMobile, "Learn", UsesAi: true),
        new("m-learn-journey", "Learn: Journey", ReflectPlatform.ReflectMobile, "Learn", UsesAi: true),
        new("m-reflection-journals", "Reflection Journals", ReflectPlatform.ReflectMobile, "Reflect", UsesAi: true),
        // Reflect XR Home
        new("xr-containment", "Containment", ReflectPlatform.ReflectXR, "Creative Calm"),
        new("xr-rhythmic-relaxation", "Rhythmic Relaxation", ReflectPlatform.ReflectXR, "Creative Calm"),
        new("xr-color-psychology", "Color Psychology", ReflectPlatform.ReflectXR, "Creative Calm"),
        new("xr-flow", "Flow", ReflectPlatform.ReflectXR, "Creative Calm"),
        new("xr-emotional-expression", "Emotional Expression", ReflectPlatform.ReflectXR, "Explore Emotions"),
        new("xr-self-compassion", "Self Compassion", ReflectPlatform.ReflectXR, "Explore Emotions"),
        new("xr-catharsis", "Catharsis", ReflectPlatform.ReflectXR, "Explore Emotions"),
        new("xr-portal-subconscious", "Portal to the Subconscious", ReflectPlatform.ReflectXR, "Explore Emotions"),
        new("xr-present-self-outside", "Present Self Outside", ReflectPlatform.ReflectXR, "Explore Self"),
        new("xr-present-self-inside", "Present Self Inside", ReflectPlatform.ReflectXR, "Explore Self"),
        new("xr-personal-strengths", "Personal Strengths", ReflectPlatform.ReflectXR, "Explore Self"),
        new("xr-future-self", "Future Self", ReflectPlatform.ReflectXR, "Explore Self"),
        new("xr-appreciation-of-life", "Appreciation of Life", ReflectPlatform.ReflectXR, "Explore Journey"),
        new("xr-hope-tree", "Hope Tree", ReflectPlatform.ReflectXR, "Explore Journey"),
        new("xr-new-pathways", "New Pathways", ReflectPlatform.ReflectXR, "Explore Journey"),
        new("xr-finding-balance", "Finding Balance", ReflectPlatform.ReflectXR, "Explore Journey"),
        new(TutorialKey, "Training Tutorial", ReflectPlatform.ReflectXR, "Getting Started", IsTutorial: true),
    };

    private static readonly Dictionary<string, ModuleInfo> ByKey = All.ToDictionary(m => m.Key);

    /// <summary>Groups in catalog order, per platform.</summary>
    public static IReadOnlyList<(ReflectPlatform Platform, string Group)> Groups { get; } =
        All.Select(m => (m.Platform, m.Group)).Distinct().ToList();

    public static ModuleInfo Get(string key) =>
        ByKey.TryGetValue(key ?? string.Empty, out var m) ? m : new ModuleInfo(key ?? string.Empty, key ?? "Unknown module", ReflectPlatform.ReflectMobile, "Other");

    public static bool Exists(string key) => key != null && ByKey.ContainsKey(key);
}

public sealed record ObservationTypeInfo(string Code, string Display, string Unit, ObservationCategory Category, decimal? NormalLow, decimal? NormalHigh);

/// <summary>Registry of observation codes. Adding an RPM measure means adding a line here.</summary>
public static class ObservationCatalog
{
    public const string HeartRate = "8867-4";
    public const string HeartRateVariability = "80404-7";

    public static readonly IReadOnlyList<ObservationTypeInfo> All = new List<ObservationTypeInfo>
    {
        new(HeartRate, "Heart rate", "beats/min", ObservationCategory.Physiologic, 50, 100),
        new(HeartRateVariability, "Heart rate variability (SDNN)", "ms", ObservationCategory.Physiologic, null, null),
    };

    public static ObservationTypeInfo Get(string code) =>
        All.FirstOrDefault(o => o.Code == code) ?? new ObservationTypeInfo(code, code, string.Empty, ObservationCategory.Physiologic, null, null);
}
