using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Communications;

public class CommunicationSaveData
{
    public CareCommunication Communication { get; set; }
}

/// <summary>
/// Documents a contact that happened outside the dashboard (Zoom, phone, the patient portal). The dashboard only records it.
/// </summary>
public class CommunicationCreateViewModel
{
    public const int MaxBackdateDays = 60;

    [Required(ErrorMessage = "Choose a patient.")]
    [Display(Name = "Patient")]
    public Guid? PatientId { get; set; }

    [Display(Name = "Type of contact")]
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.VideoCall;

    [Required(ErrorMessage = "Enter the date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateOnly? OccurredDate { get; set; }

    [Required(ErrorMessage = "Enter the time.")]
    [DataType(DataType.Time)]
    [Display(Name = "Time (Eastern)")]
    public TimeOnly? OccurredTime { get; set; }

    [Required(ErrorMessage = "Enter the duration.")]
    [Range(1, 120, ErrorMessage = "Duration must be between 1 and 120 minutes.")]
    [Display(Name = "Duration (minutes)")]
    public int? DurationMinutes { get; set; }

    [Required(ErrorMessage = "Add a short summary.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Summary must be 10 to 1,000 characters.")]
    [Display(Name = "Summary")]
    public string Summary { get; set; } = string.Empty;

    [Display(Name = "We discussed the patient's Reflect data (sessions, mood, assessments)")]
    public bool DiscussedRemoteData { get; set; } = true;

    public bool ReturnToPatient { get; set; }
    public string LockedPatientName { get; set; } = string.Empty;
    public List<PatientOption> Patients { get; set; } = new();

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, Guid? patientId, CancellationToken ct)
    {
        var local = context.NowUtc.ToEastern();
        OccurredDate = context.Today;
        OccurredTime = new TimeOnly(local.Hour, local.Minute);
        await LoadOptions(repos, context, patientId, ct);
        if (patientId.HasValue && !string.IsNullOrEmpty(LockedPatientName))
        {
            PatientId = patientId;
            ReturnToPatient = true;
        }
    }

    public async Task<CommunicationSaveData> PrepareDataForSave(ProviderDashboardRepositories repos, ProviderContext context, ModelStateDictionary modelState, CancellationToken ct)
    {
        await LoadOptions(repos, context, ReturnToPatient ? PatientId : null, ct);

        Patient patient = null;
        if (PatientId.HasValue)
        {
            patient = await repos.PatientRepo.GetForProviderAsync(PatientId.Value, context.ProviderId, ct);
            if (patient == null) modelState.AddModelError(nameof(PatientId), ProviderDashboardMessages.PatientNotFound);
        }

        DateTimeOffset occurredAt = default;
        if (OccurredDate.HasValue && OccurredTime.HasValue)
        {
            occurredAt = OccurredDate.Value.StartOfEasternDayUtc().Add(OccurredTime.Value.ToTimeSpan());
            if (occurredAt > context.NowUtc.AddMinutes(5))
                modelState.AddModelError(nameof(OccurredDate), "The contact can't be in the future.");
            else if (OccurredDate.Value < context.Today.AddDays(-MaxBackdateDays))
                modelState.AddModelError(nameof(OccurredDate), $"Log contacts within {MaxBackdateDays} days.");
        }
        if (!modelState.IsValid) return null;

        return new CommunicationSaveData
        {
            Communication = new CareCommunication
            {
                PatientId = patient.Id,
                ProviderId = context.ProviderId,
                Channel = Channel,
                OccurredAt = occurredAt,
                DurationMinutes = DurationMinutes.Value,
                Summary = Summary.Trim(),
                DiscussedRemoteData = DiscussedRemoteData,
                DateCreated = context.NowUtc
            }
        };
    }

    private async Task LoadOptions(ProviderDashboardRepositories repos, ProviderContext context, Guid? lockedPatientId, CancellationToken ct)
    {
        var patients = await repos.PatientRepo.GetByProviderAsync(context.ProviderId, ct);
        Patients = PatientOptions.From(patients);
        LockedPatientName = lockedPatientId.HasValue ? patients.FirstOrDefault(p => p.Id == lockedPatientId)?.FullName ?? string.Empty : string.Empty;
    }
}
