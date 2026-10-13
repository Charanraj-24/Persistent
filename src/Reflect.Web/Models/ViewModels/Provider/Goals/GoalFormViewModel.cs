using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;

namespace Reflect.Web.Models.ViewModels.Provider.Goals;

public class GoalSaveData
{
    public TreatmentGoal Goal { get; set; }
}

/// <summary>One-page create/edit form for a treatment goal.</summary>
public class GoalFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid PatientId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a goal title.")]
    [StringLength(120)]
    [Display(Name = "Goal")]
    public string Title { get; set; } = string.Empty;

    [StringLength(600)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter what you'll measure.")]
    [StringLength(80)]
    [Display(Name = "Measure")]
    public string MeasureName { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Unit")]
    public string Unit { get; set; } = string.Empty;

    [Display(Name = "Progress means the value goes")]
    public GoalDirection Direction { get; set; } = GoalDirection.Decrease;

    [Required]
    [Range(0, 10000)]
    [Display(Name = "Baseline")]
    public decimal? BaselineValue { get; set; }

    [Required]
    [Range(0, 10000)]
    [Display(Name = "Target")]
    public decimal? TargetValue { get; set; }

    [Required]
    [Range(0, 10000)]
    [Display(Name = "Current value")]
    public decimal? CurrentValue { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Start date")]
    public DateOnly? StartDate { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Target date")]
    public DateOnly? TargetDate { get; set; }

    [Display(Name = "Status")]
    public GoalStatus Status { get; set; } = GoalStatus.Active;

    public bool IsEdit => Id.HasValue;

    public static readonly IReadOnlyList<(string Title, string Measure, string Unit, GoalDirection Direction)> Templates = new[]
    {
        ("Reduce depressive symptoms", "PHQ-9 score", "points", GoalDirection.Decrease),
        ("Reduce anxiety symptoms", "GAD-7 score", "points", GoalDirection.Decrease),
        ("Build a consistent creative practice", "Sessions per week", "sessions/week", GoalDirection.Increase),
        ("Improve mood after sessions", "Average mood change", "points", GoalDirection.Increase)
    };

    public async Task PrepareDataForCreate(ProviderDashboardRepositories repos, ProviderContext context, Guid patientId, CancellationToken ct)
    {
        var patient = await LoadPatient(repos, context, patientId, ct);
        PatientId = patient.Id;
        PatientName = patient.FullName;
        StartDate = context.Today;
        TargetDate = context.Today.AddDays(90);
    }

    public async Task PrepareDataForEdit(ProviderDashboardRepositories repos, ProviderContext context, Guid goalId, CancellationToken ct)
    {
        var goal = await repos.TreatmentGoalRepo.GetByIdAsync(goalId, ct) ?? throw new EntityNotFoundException(ProviderDashboardMessages.GoalNotFound);
        var patient = await LoadPatient(repos, context, goal.PatientId, ct);
        Id = goal.Id;
        PatientId = patient.Id;
        PatientName = patient.FullName;
        Title = goal.Title;
        Description = goal.Description;
        MeasureName = goal.MeasureName;
        Unit = goal.Unit;
        Direction = goal.Direction;
        BaselineValue = goal.BaselineValue;
        TargetValue = goal.TargetValue;
        CurrentValue = goal.CurrentValue;
        StartDate = goal.StartDate;
        TargetDate = goal.TargetDate;
        Status = goal.Status;
    }

    /// <summary>Validates the form. Returns null (and fills ModelState) when invalid. Never saves.</summary>
    public async Task<GoalSaveData> PrepareDataForSave(ProviderDashboardRepositories repos, ProviderContext context, ModelStateDictionary modelState, CancellationToken ct)
    {
        var patient = await LoadPatient(repos, context, PatientId, ct);
        PatientName = patient.FullName;

        if (StartDate.HasValue && TargetDate.HasValue && TargetDate <= StartDate)
            modelState.AddModelError(nameof(TargetDate), "Target date must be after the start date.");
        if (BaselineValue.HasValue && TargetValue.HasValue)
        {
            if (Direction == GoalDirection.Decrease && TargetValue >= BaselineValue)
                modelState.AddModelError(nameof(TargetValue), "For a goal that decreases, the target must be below the baseline.");
            if (Direction == GoalDirection.Increase && TargetValue <= BaselineValue)
                modelState.AddModelError(nameof(TargetValue), "For a goal that increases, the target must be above the baseline.");
        }
        if (!modelState.IsValid) return null;

        TreatmentGoal goal;
        if (IsEdit)
        {
            goal = await repos.TreatmentGoalRepo.GetByIdAsync(Id.Value, ct);
            if (goal == null || goal.PatientId != patient.Id) throw new EntityNotFoundException(ProviderDashboardMessages.GoalNotFound);
            goal.DateUpdated = context.NowUtc;
        }
        else
        {
            goal = new TreatmentGoal { PatientId = patient.Id, ProviderId = context.ProviderId, DateCreated = context.NowUtc };
        }

        goal.Title = Title.Trim();
        goal.Description = (Description ?? string.Empty).Trim();
        goal.MeasureName = MeasureName.Trim();
        goal.Unit = (Unit ?? string.Empty).Trim();
        goal.Direction = Direction;
        goal.BaselineValue = BaselineValue.Value;
        goal.TargetValue = TargetValue.Value;
        goal.CurrentValue = CurrentValue.Value;
        goal.StartDate = StartDate.Value;
        goal.TargetDate = TargetDate.Value;
        goal.Status = Status;
        return new GoalSaveData { Goal = goal };
    }

    private static async Task<Patient> LoadPatient(ProviderDashboardRepositories repos, ProviderContext context, Guid patientId, CancellationToken ct) =>
        await repos.PatientRepo.GetForProviderAsync(patientId, context.ProviderId, ct)
        ?? throw new EntityNotFoundException(ProviderDashboardMessages.PatientNotFound);
}
