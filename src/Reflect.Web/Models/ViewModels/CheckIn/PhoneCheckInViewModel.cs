using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Api.Models;
using Reflect.Web.Services.CheckIn;
using Reflect.Web.Services.Ingestion;

namespace Reflect.Web.Models.ViewModels.CheckIn;

/// <summary>
/// DEMO: public phone check-in reached from the QR code on the Live page. Posts one session with before/after
/// mood for the synthetic client the presenter picked. Shows no client information.
/// </summary>
public class PhoneCheckInViewModel
{
    public static readonly string[] EmotionChoices =
        { "calm", "relaxed", "hopeful", "grateful", "proud", "curious", "focused", "tired", "anxious", "sad", "overwhelmed", "angry" };

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose what you did.")]
    [Display(Name = "What did you do?")]
    public string ModuleKey { get; set; } = string.Empty;

    [Range(1, 10)] public int MoodBefore { get; set; } = 5;
    [Range(1, 10)] public int MoodAfter { get; set; } = 6;
    [Range(1, 10)] public int StressAfter { get; set; } = 4;

    public List<string> EmotionTags { get; set; } = new();

    public bool CodeValid { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<SelectListItem> ModuleOptions { get; set; } = new();

    public void PrepareData(PhoneCheckInService service, DateTimeOffset now)
    {
        CodeValid = service.Find(Code, now) != null;
        var groups = new Dictionary<string, SelectListGroup>();
        ModuleOptions = ModuleCatalog.All
            .Select(m =>
            {
                var label = $"{m.PlatformLabel}: {m.Group}";
                if (!groups.TryGetValue(label, out var g)) groups[label] = g = new SelectListGroup { Name = label };
                return new SelectListItem(m.Name, m.Key) { Group = g };
            })
            .ToList();
    }

    /// <summary>Returns null on success, or a message to show.</summary>
    public async Task<string> PrepareDataForSave(PhoneCheckInService service, IngestionService ingestion, string deviceKey, DateTimeOffset now, CancellationToken ct)
    {
        var session = service.Find(Code, now);
        if (session == null) return "This check-in code has expired. Ask the presenter for a new one.";
        if (!service.TryConsumeDeviceQuota(deviceKey, now)) return "Thanks! You've already checked in a few times. Try again in a couple of minutes.";
        var module = ModuleCatalog.All.FirstOrDefault(m => m.Key == ModuleKey);
        if (module == null) return "Choose what you did.";

        var result = await ingestion.RecordSessionAsync(new SessionIngestRequest
        {
            ClientUserId = session.ClientUserId,
            Platform = module.Platform == ReflectPlatform.ReflectXR ? "xr" : "mobile",
            ModuleKey = module.Key,
            StartedAt = now.AddMinutes(-12),
            EndedAt = now,
            Completed = true,
            ArtworksCreated = module.Platform == ReflectPlatform.ReflectXR || module.Group == "Create" ? 1 : 0,
            MoodBefore = MoodBefore,
            MoodAfter = MoodAfter,
            StressAfter = StressAfter,
            EmotionTags = EmotionTags.Where(t => EmotionChoices.Contains(t)).Distinct().Take(3).ToList()
        }, "phone", ct);
        if (!result.Ok) return "That check-in could not be saved. Please try again.";
        service.CountSubmission(session);
        return null;
    }
}
