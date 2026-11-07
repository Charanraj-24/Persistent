using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Api.Models;

namespace Reflect.Web.Services.Ingestion;

public sealed class IngestResult
{
    public bool Ok => Errors.Count == 0;
    public List<string> Errors { get; } = new();
    public Guid EventId { get; set; }
    public string Summary { get; set; } = string.Empty;

    public static IngestResult Fail(params string[] errors)
    {
        var r = new IngestResult();
        r.Errors.AddRange(errors);
        return r;
    }
}

/// <summary>
/// Validates incoming app data, maps it to entities, stores it and records an <see cref="IngestEvent"/>.
/// Used by the ingestion API and by the demo simulator, so both take exactly the same path.
/// </summary>
public class IngestionService
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(5);
    private readonly IProviderClientRepository _clients;
    private readonly IAppSessionRepository _sessions;
    private readonly IMoodCheckInRepository _checkIns;
    private readonly IAssessmentResultRepository _assessments;
    private readonly IObservationRepository _observations;
    private readonly IIngestEventRepository _events;

    public IngestionService(IProviderClientRepository clients, IAppSessionRepository sessions, IMoodCheckInRepository checkIns,
        IAssessmentResultRepository assessments, IObservationRepository observations, IIngestEventRepository events)
    {
        _clients = clients;
        _sessions = sessions;
        _checkIns = checkIns;
        _assessments = assessments;
        _observations = observations;
        _events = events;
    }

    public async Task<IngestResult> RecordSessionAsync(SessionIngestRequest r, string source, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var client = await FindClientAsync(r.ClientUserId, ct);
        if (client == null) return IngestResult.Fail("Unknown clientUserId.");

        var platform = r.Platform?.Trim().ToLowerInvariant() switch
        {
            "xr" or "vr" or "reflectxr" => (ReflectPlatform?)ReflectPlatform.ReflectXR,
            "mobile" or "reflectmobile" => ReflectPlatform.ReflectMobile,
            _ => null
        };
        var errors = new List<string>();
        if (platform == null) errors.Add("platform must be \"xr\" or \"mobile\".");
        var module = ModuleCatalog.All.FirstOrDefault(m => m.Key == r.ModuleKey);
        if (module == null) errors.Add("moduleKey is not a known module.");
        else if (platform != null && module.Platform != platform) errors.Add("moduleKey does not belong to that platform.");

        var ended = r.EndedAt ?? now;
        var started = r.StartedAt ?? ended.AddMinutes(-15);
        if (ended > now + ClockSkew) errors.Add("endedAt is in the future.");
        if (ended < started) errors.Add("endedAt is before startedAt.");
        if ((ended - started) > TimeSpan.FromHours(4)) errors.Add("Sessions longer than 4 hours are rejected.");
        CheckScale(r.MoodBefore, "moodBefore", errors);
        CheckScale(r.MoodAfter, "moodAfter", errors);
        CheckScale(r.StressBefore, "stressBefore", errors);
        CheckScale(r.StressAfter, "stressAfter", errors);
        var tags = (r.EmotionTags ?? new List<string>())
            .Select(t => new string((t ?? string.Empty).Trim().ToLowerInvariant().Where(ch => char.IsLetter(ch) || ch == '-').Take(24).ToArray()))
            .Where(t => t.Length > 0).Distinct().Take(5).ToList();
        if (errors.Count > 0) return IngestResult.Fail(errors.ToArray());

        var session = new AppSession
        {
            Source = source,
            ClientUserId = client.ClientUserId, Platform = platform!.Value, ModuleKey = module!.Key,
            StartedAt = started, EndedAt = ended, Completed = r.Completed,
            ArtworksCreated = Math.Clamp(r.ArtworksCreated, 0, 50), ReflectionsWritten = Math.Clamp(r.ReflectionsWritten, 0, 50)
        };
        await _sessions.AddAsync(session, ct);

        if (r.MoodBefore.HasValue || r.StressBefore.HasValue)
        {
            await _checkIns.AddAsync(new MoodCheckIn
            {
                ClientUserId = client.ClientUserId, AppSessionId = session.Id, Phase = CheckInPhase.PreSession,
                RecordedAt = started.AddSeconds(-30), MoodScore = r.MoodBefore ?? 5, StressScore = r.StressBefore ?? 5
            }, ct);
        }
        if (r.MoodAfter.HasValue || r.StressAfter.HasValue || tags.Count > 0)
        {
            await _checkIns.AddAsync(new MoodCheckIn
            {
                ClientUserId = client.ClientUserId, AppSessionId = session.Id, Phase = CheckInPhase.PostSession,
                RecordedAt = ended.AddSeconds(20), MoodScore = r.MoodAfter ?? r.MoodBefore ?? 5,
                StressScore = r.StressAfter ?? r.StressBefore ?? 5, EmotionTags = string.Join(",", tags)
            }, ct);
        }

        var minutes = (int)Math.Round(session.DurationMinutes);
        var app = platform == ReflectPlatform.ReflectXR ? "Reflect XR" : "Reflect Mobile";
        var mood = r.MoodBefore.HasValue && r.MoodAfter.HasValue ? $", mood {r.MoodBefore} to {r.MoodAfter}" : string.Empty;
        var verb = r.Completed ? "Finished" : "Left";
        return await RecordEventAsync(client, "session", $"{verb} {module.Name} in {app} ({minutes} min{mood})", source, false, ct);
    }

    public async Task<IngestResult> RecordAssessmentAsync(AssessmentIngestRequest r, string source, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var client = await FindClientAsync(r.ClientUserId, ct);
        if (client == null) return IngestResult.Fail("Unknown clientUserId.");

        var instrument = r.Instrument?.Trim().ToLowerInvariant().Replace("-", string.Empty) switch
        {
            "phq9" => (AssessmentInstrument?)AssessmentInstrument.Phq9,
            "gad7" => AssessmentInstrument.Gad7,
            _ => null
        };
        if (instrument == null) return IngestResult.Fail("instrument must be \"phq9\" or \"gad7\".");
        var max = AssessmentScoring.MaxScore(instrument.Value);
        var errors = new List<string>();
        if (r.TotalScore < 0 || r.TotalScore > max) errors.Add($"totalScore must be 0-{max}.");
        if (instrument == AssessmentInstrument.Phq9 && (r.SafetyItemScore < 0 || r.SafetyItemScore > 3)) errors.Add("safetyItemScore must be 0-3.");
        var completed = r.CompletedAt ?? now;
        if (completed > now + ClockSkew) errors.Add("completedAt is in the future.");
        if (errors.Count > 0) return IngestResult.Fail(errors.ToArray());

        var safety = instrument == AssessmentInstrument.Phq9 ? r.SafetyItemScore : 0;
        await _assessments.AddAsync(new AssessmentResult
        {
            Source = source,
            ClientUserId = client.ClientUserId, Instrument = instrument.Value, TotalScore = r.TotalScore,
            SafetyItemScore = safety, CompletedAt = completed
        }, ct);

        var name = AssessmentScoring.Name(instrument.Value);
        var severity = AssessmentScoring.Severity(instrument.Value, r.TotalScore).ToLowerInvariant();
        var summary = $"Completed {name}: {r.TotalScore} ({severity}){(safety > 0 ? ", item 9 endorsed" : string.Empty)}";
        return await RecordEventAsync(client, "assessment", summary, source, safety > 0, ct);
    }

    public async Task<IngestResult> RecordObservationAsync(ObservationIngestRequest r, string source, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var client = await FindClientAsync(r.ClientUserId, ct);
        if (client == null) return IngestResult.Fail("Unknown clientUserId.");

        var info = ObservationCatalog.All.FirstOrDefault(o => o.Code == r.Code);
        if (info == null) return IngestResult.Fail("code is not a registered observation code.");
        var effective = r.EffectiveAt ?? now;
        if (effective > now + ClockSkew) return IngestResult.Fail("effectiveAt is in the future.");
        if (r.Value < 0 || r.Value > 1000) return IngestResult.Fail("value is out of range.");
        var context = r.Context?.Trim().ToLowerInvariant() is "pre" or "post" ? r.Context.Trim().ToLowerInvariant() : string.Empty;

        await _observations.AddAsync(new Observation
        {
            Source = source,
            ClientUserId = client.ClientUserId, Category = info.Category, Code = info.Code, Value = r.Value, Unit = info.Unit,
            EffectiveAt = effective, Context = context,
            SourceDevice = string.IsNullOrWhiteSpace(r.SourceDevice) ? "Unknown device" : r.SourceDevice.Trim()[..Math.Min(60, r.SourceDevice.Trim().Length)]
        }, ct);

        var outOfRange = info.NormalHigh.HasValue && r.Value > info.NormalHigh.Value || info.NormalLow.HasValue && r.Value < info.NormalLow.Value;
        var summary = $"{info.Display}: {r.Value:0} {info.Unit}{(outOfRange ? " (outside normal range)" : string.Empty)}";
        return await RecordEventAsync(client, "observation", summary, source, false, ct);
    }

    private async Task<ProviderClient> FindClientAsync(Guid clientUserId, CancellationToken ct)
    {
        try { return await _clients.GetByClientUserIdAsync(clientUserId, ct); }
        catch (EntityNotFoundException) { return null; }
    }

    private async Task<IngestResult> RecordEventAsync(ProviderClient client, string kind, string summary, string source, bool urgent, CancellationToken ct)
    {
        var e = new IngestEvent
        {
            ClientUserId = client.ClientUserId, Kind = kind, Summary = summary, Source = source,
            IsUrgent = urgent, ReceivedAt = DateTimeOffset.UtcNow
        };
        await _events.AddAsync(e, ct);
        return new IngestResult { EventId = e.Id, Summary = summary };
    }

    private static void CheckScale(int? value, string name, List<string> errors)
    {
        if (value.HasValue && (value < 1 || value > 10)) errors.Add($"{name} must be 1-10.");
    }
}
