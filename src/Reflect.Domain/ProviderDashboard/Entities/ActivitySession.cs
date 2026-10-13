using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Domain.ProviderDashboard.Entities;

/// <summary>One patient session in Reflect XR (VR headset) or Reflect Mobile.</summary>
public class ActivitySession : BaseEntity
{
    public Guid PatientId { get; set; }
    public ReflectPlatform Platform { get; set; }
    public SessionSetting Setting { get; set; } = SessionSetting.Remote;
    /// <summary>Key from <see cref="Catalogs.ModuleCatalog"/>.</summary>
    public string ModuleKey { get; set; } = string.Empty;
    /// <summary>Pathway inside a module, for example "Appreciation for Life" in Explore Journey. Empty when not applicable.</summary>
    public string Pathway { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    /// <summary>Self-reported mood before the session, 1 (very low) to 10 (very good). Null when skipped.</summary>
    public int? MoodBefore { get; set; }
    public int? MoodAfter { get; set; }
    public int CreationsCount { get; set; }
    public bool ReflectionWritten { get; set; }
    public bool Completed { get; set; } = true;

    public int DurationMinutes => (int)Math.Round((EndedAt - StartedAt).TotalMinutes);
}
