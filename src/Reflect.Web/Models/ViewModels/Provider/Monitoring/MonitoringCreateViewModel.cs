using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Monitoring;

public class MonitoringSaveData
{
    public MonitoringTimeEntry Entry { get; set; }
}

/// <summary>Manual entry for monitoring work done outside the dashboard. A note is required so the time is defensible.</summary>
public class MonitoringCreateViewModel
{
    [Required(ErrorMessage = "Choose a patient.")]
    [Display(Name = "Patient")]
    public Guid? PatientId { get; set; }

    [Required(ErrorMessage = "Enter the date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Enter the minutes.")]
    [Range(1, MonitoringTimePolicy.MaxManualEntryMinutes, ErrorMessage = "Minutes must be between 1 and 120.")]
    [Display(Name = "Minutes")]
    public int Minutes { get; set; } = 5;

    [Required(ErrorMessage = "Describe what you reviewed.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Note must be 10 to 500 characters.")]
    [Display(Name = "What did you review?")]
    public string Note { get; set; } = string.Empty;

    public bool ReturnToPatient { get; set; }
    public string LockedPatientName { get; set; } = string.Empty;
    public List<PatientOption> Patients { get; set; } = new();
    public string MinDate { get; set; } = string.Empty;
    public string MaxDate { get; set; } = string.Empty;

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, Guid? patientId, CancellationToken ct)
    {
        Date = context.Today;
        await LoadOptions(repos, context, patientId, ct);
        if (patientId.HasValue && !string.IsNullOrEmpty(LockedPatientName))
        {
            PatientId = patientId;
            ReturnToPatient = true;
        }
    }

    public async Task<MonitoringSaveData> PrepareDataForSave(ProviderDashboardRepositories repos, ProviderContext context, ModelStateDictionary modelState, CancellationToken ct)
    {
        await LoadOptions(repos, context, ReturnToPatient ? PatientId : null, ct);

        Patient patient = null;
        if (PatientId.HasValue)
        {
            patient = await repos.PatientRepo.GetForProviderAsync(PatientId.Value, context.ProviderId, ct);
            if (patient == null) modelState.AddModelError(nameof(PatientId), ProviderDashboardMessages.PatientNotFound);
        }

        // Manual time can only be added to the current billing period, and not in the future.
        var period = BillingPeriod.Containing(context.Today);
        if (Date.HasValue && (Date.Value > context.Today || !period.Contains(Date.Value)))
            modelState.AddModelError(nameof(Date), $"Choose a date in {period.Label}, up to today.");
        if (!modelState.IsValid) return null;

        var start = Date.Value == context.Today
            ? context.NowUtc.AddMinutes(-Minutes)
            : Date.Value.StartOfEasternDayUtc().AddHours(12);
        return new MonitoringSaveData
        {
            Entry = new MonitoringTimeEntry
            {
                PatientId = patient.Id,
                ProviderId = context.ProviderId,
                Source = MonitoringTimeSource.ManualEntry,
                StartedAt = start,
                LastActivityAt = start.AddMinutes(Minutes),
                ActiveSeconds = Minutes * 60,
                Note = Note.Trim(),
                IsClosed = true,
                DateCreated = context.NowUtc
            }
        };
    }

    private async Task LoadOptions(ProviderDashboardRepositories repos, ProviderContext context, Guid? lockedPatientId, CancellationToken ct)
    {
        var patients = (await repos.PatientRepo.GetByProviderAsync(context.ProviderId, ct)).Where(p => p.RtmConsentOn.HasValue).ToList();
        Patients = PatientOptions.From(patients);
        LockedPatientName = lockedPatientId.HasValue ? patients.FirstOrDefault(p => p.Id == lockedPatientId)?.FullName ?? string.Empty : string.Empty;
        var period = BillingPeriod.Containing(context.Today);
        MinDate = period.Start.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        MaxDate = context.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }
}
