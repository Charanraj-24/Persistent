using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Api.Models;
using Reflect.Web.Services.Ingestion;

namespace Reflect.Web.Services.Simulation;

public sealed record DemoScenario(string Key, string Label, string Description, string Icon, bool IsSafety = false);

/// <summary>
/// DEMO ONLY. Builds realistic app payloads ("what the headset or phone would send") and pushes them through
/// <see cref="IngestionService"/>, the same path as the real ingestion API.
/// </summary>
public static class DemoScenarios
{
    public static readonly IReadOnlyList<DemoScenario> All = new[]
    {
        new DemoScenario("vr-session", "VR session, mood improves", "Finishes a Reflect XR module and feels better afterward.", "ri-goggles-line"),
        new DemoScenario("mobile-session", "Mobile session", "Uses a Reflect Mobile module for a few minutes.", "ri-smartphone-line"),
        new DemoScenario("difficult-session", "Difficult session", "Explore Emotions surfaces hard feelings; mood drops.", "ri-emotion-unhappy-line"),
        new DemoScenario("phq9-better", "PHQ-9, improved", "Completes a PHQ-9 in the mild range.", "ri-arrow-down-line"),
        new DemoScenario("gad7", "GAD-7", "Completes a GAD-7.", "ri-survey-line"),
        new DemoScenario("heart-rate", "Heart-rate reading (RPM preview)", "A paired wearable sends a heart-rate reading. Planned extension.", "ri-heart-pulse-line"),
        new DemoScenario("phq9-safety", "PHQ-9, item 9 endorsed", "Completes a PHQ-9 reporting thoughts of self-harm. Raises an urgent flag.", "ri-alarm-warning-line", IsSafety: true),
    };

    private static readonly string[] Positive = { "calm", "hopeful", "relieved", "grateful", "proud", "peaceful" };
    private static readonly string[] Hard = { "grief", "sad", "overwhelmed", "angry", "anxious" };

    public static Task<IngestResult> RunAsync(string key, ProviderClient client, IngestionService ingestion, Random rng, string source, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        switch (key)
        {
            case "vr-session":
            case "mobile-session":
            {
                var xr = key == "vr-session";
                var pool = ModuleCatalog.All.Where(m => !m.IsTutorial && m.Platform == (xr ? ReflectPlatform.ReflectXR : ReflectPlatform.ReflectMobile)).ToList();
                var module = pool[rng.Next(pool.Count)];
                var minutes = xr ? rng.Next(12, 31) : rng.Next(4, 16);
                var before = rng.Next(3, 7);
                return ingestion.RecordSessionAsync(new SessionIngestRequest
                {
                    ClientUserId = client.ClientUserId, Platform = xr ? "xr" : "mobile", ModuleKey = module.Key,
                    StartedAt = now.AddMinutes(-minutes), EndedAt = now, Completed = true, ArtworksCreated = rng.Next(0, 3),
                    MoodBefore = before, MoodAfter = Math.Min(10, before + rng.Next(1, 4)),
                    StressBefore = rng.Next(5, 9), StressAfter = rng.Next(2, 6),
                    EmotionTags = Positive.OrderBy(_ => rng.Next()).Take(2).ToList()
                }, source, ct);
            }
            case "difficult-session":
            {
                var minutes = rng.Next(15, 28);
                return ingestion.RecordSessionAsync(new SessionIngestRequest
                {
                    ClientUserId = client.ClientUserId, Platform = "xr", ModuleKey = "xr-catharsis",
                    StartedAt = now.AddMinutes(-minutes), EndedAt = now, Completed = true,
                    MoodBefore = 5, MoodAfter = 3, StressBefore = 5, StressAfter = 7,
                    EmotionTags = Hard.OrderBy(_ => rng.Next()).Take(2).ToList()
                }, source, ct);
            }
            case "phq9-better":
                return ingestion.RecordAssessmentAsync(new AssessmentIngestRequest { ClientUserId = client.ClientUserId, Instrument = "phq9", TotalScore = rng.Next(5, 9), CompletedAt = now }, source, ct);
            case "gad7":
                return ingestion.RecordAssessmentAsync(new AssessmentIngestRequest { ClientUserId = client.ClientUserId, Instrument = "gad7", TotalScore = rng.Next(4, 13), CompletedAt = now }, source, ct);
            case "heart-rate":
                return ingestion.RecordObservationAsync(new ObservationIngestRequest
                {
                    ClientUserId = client.ClientUserId, Code = ObservationCatalog.HeartRate, Value = rng.Next(64, 96),
                    Unit = "beats/min", EffectiveAt = now, SourceDevice = "Paired wearable (simulated)"
                }, source, ct);
            case "phq9-safety":
                return ingestion.RecordAssessmentAsync(new AssessmentIngestRequest { ClientUserId = client.ClientUserId, Instrument = "phq9", TotalScore = rng.Next(17, 22), SafetyItemScore = 1, CompletedAt = now }, source, ct);
            default:
                return Task.FromResult(IngestResult.Fail("Unknown scenario."));
        }
    }

    /// <summary>Weighted pick for the auto-simulator. Never picks the safety scenario on its own.</summary>
    public static string PickAutomatic(Random rng)
    {
        var roll = rng.NextDouble();
        // RTM data first (sessions, check-ins, assessments); heart rate is a rare RPM-preview event.
        return roll < 0.48 ? "vr-session" : roll < 0.80 ? "mobile-session" : roll < 0.87 ? "difficult-session"
            : roll < 0.93 ? "gad7" : roll < 0.97 ? "phq9-better" : "heart-rate";
    }
}
