namespace Reflect.Domain.Entities.ProviderDashboard;

/// <summary>
/// CLINICAL (therapeutic response). A short self-rating the client gives right before or after a session,
/// plus optional emotion tags chosen in-app. Proposed new in-app data point.
/// </summary>
public class MoodCheckIn : BaseEntity
{
    public Guid ClientUserId { get; set; }
    public Guid? AppSessionId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    public CheckInPhase Phase { get; set; }
    /// <summary>1 (very low) to 10 (very good).</summary>
    public int MoodScore { get; set; }
    /// <summary>1 (very calm) to 10 (very stressed).</summary>
    public int StressScore { get; set; }
    /// <summary>Comma-separated emotion words, for example "calm,hopeful".</summary>
    public string EmotionTags { get; set; } = string.Empty;
}
