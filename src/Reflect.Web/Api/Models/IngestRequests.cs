namespace Reflect.Web.Api.Models;

// Contract for data coming from Reflect XR and Reflect Mobile (in production, forwarded by Reflect.Api or a
// BigQuery export job). JSON, camelCase. Times are ISO-8601 with offset; omitted times default to "now".

/// <summary>POST /api/ingest/sessions: one completed (or abandoned) module session, with optional check-ins.</summary>
public class SessionIngestRequest
{
    public Guid ClientUserId { get; set; }
    /// <summary>"xr" or "mobile".</summary>
    public string Platform { get; set; } = string.Empty;
    public string ModuleKey { get; set; } = string.Empty;
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public bool Completed { get; set; } = true;
    public int ArtworksCreated { get; set; }
    public int ReflectionsWritten { get; set; }
    public int? MoodBefore { get; set; }
    public int? MoodAfter { get; set; }
    public int? StressBefore { get; set; }
    public int? StressAfter { get; set; }
    public List<string> EmotionTags { get; set; } = new();
}

/// <summary>POST /api/ingest/assessments: a PHQ-9 or GAD-7 completed in the app.</summary>
public class AssessmentIngestRequest
{
    public Guid ClientUserId { get; set; }
    /// <summary>"phq9" or "gad7".</summary>
    public string Instrument { get; set; } = string.Empty;
    public int TotalScore { get; set; }
    /// <summary>PHQ-9 item 9 (0-3). Ignored for GAD-7.</summary>
    public int SafetyItemScore { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

/// <summary>POST /api/ingest/observations: a physiologic reading (for example heart rate, LOINC 8867-4).</summary>
public class ObservationIngestRequest
{
    public Guid ClientUserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset? EffectiveAt { get; set; }
    public string SourceDevice { get; set; } = string.Empty;
    /// <summary>"pre", "post" or empty.</summary>
    public string Context { get; set; } = string.Empty;
}
