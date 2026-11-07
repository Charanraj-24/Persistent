namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>
/// PRODUCT ANALYTICS. One use of a Reflect module on VR or mobile.
/// In production this is derived from Firebase Analytics events exported to BigQuery
/// (session_start / module_open / module_complete / session_end) and served through the secure API.
/// </summary>
public class AppSession : BaseEntity
{
    public Guid ClientUserId { get; set; }
    public ReflectPlatform Platform { get; set; }
    public string ModuleKey { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset EndedAt { get; set; }
    public bool Completed { get; set; }
    public int ArtworksCreated { get; set; }
    public int ReflectionsWritten { get; set; }
    /// <summary>How the record arrived: "api", "phone", "live demo" or "simulator". Empty for imported history.</summary>
    public string Source { get; set; } = string.Empty;

    public double DurationMinutes => Math.Max(0, (EndedAt - StartedAt).TotalMinutes);
}
