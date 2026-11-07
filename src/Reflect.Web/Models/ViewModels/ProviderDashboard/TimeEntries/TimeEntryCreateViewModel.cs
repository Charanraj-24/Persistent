using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Messages;
using Reflect.Web.Models.Validation;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.TimeEntries;

/// <summary>Create form for manual treatment-management time or a live interaction. All fields on one page.</summary>
public class TimeEntryCreateViewModel
{
    [Required]
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose an activity.")]
    [Display(Name = "Activity")]
    public RtmActivityType? Activity { get; set; }

    [Required(ErrorMessage = "Enter the date.")]
    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    public DateOnly? Date { get; set; }

    [Required(ErrorMessage = "Enter the start time.")]
    [Display(Name = "Start time")]
    [DataType(DataType.Time)]
    public TimeOnly? StartTime { get; set; }

    [Required(ErrorMessage = "Enter the minutes spent.")]
    [Range(1, 120, ErrorMessage = "Minutes must be between 1 and 120.")]
    [Display(Name = "Minutes")]
    public int? Minutes { get; set; }

    [Display(Name = "Communication mode")]
    public CommunicationMode CommunicationMode { get; set; } = CommunicationMode.None;

    [Required(ErrorMessage = "Add a short documentation note.")]
    [StringLength(1000, ErrorMessage = "Keep the note under 1,000 characters.")]
    [Display(Name = "Documentation note")]
    public string Note { get; set; } = string.Empty;

    [MustBeTrue(ErrorMessage = "You must attest to the time before saving.")]
    [Display(Name = "I attest this time was spent on this client's remote therapeutic monitoring.")]
    public bool Attest { get; set; }

    public List<SelectListItem> ActivityOptions { get; set; } = new();
    public List<SelectListItem> ModeOptions { get; set; } = new();

    public async Task PrepareData(Guid clientId, RtmActivityType? activity, ICurrentUserService currentUser,
        IProviderProfileRepository providers, IProviderClientRepository clients, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var client = await clients.GetByIdAsync(clientId, ct);
        if (client.ProviderId != provider.Id) throw new EntityNotFoundException("Client not found.");

        ClientId = client.Id;
        ClientName = client.FullName;
        if (Activity == null) Activity = activity ?? RtmActivityType.DataReview;
        if (Date == null)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            Date = EasternTime.Today(nowUtc);
            var local = TimeZoneInfo.ConvertTime(nowUtc, TimeZoneInfo.FindSystemTimeZoneById("America/New_York"));
            StartTime = new TimeOnly(local.Hour, local.Minute);
        }
        if (Activity == RtmActivityType.InteractiveCommunication && CommunicationMode == CommunicationMode.None)
            CommunicationMode = CommunicationMode.Video;
        BuildOptions();
    }

    private void BuildOptions()
    {
        ActivityOptions = Enum.GetValues<RtmActivityType>()
            .Select(a => new SelectListItem(Clients.TimeEntryRowViewModel.ActivityLabel(a), ((int)a).ToString(), a == Activity)).ToList();
        ModeOptions = new[] { CommunicationMode.None, CommunicationMode.Video, CommunicationMode.Phone, CommunicationMode.InPerson, CommunicationMode.SecureMessage }
            .Select(m => new SelectListItem(m == CommunicationMode.None ? "Not applicable" : Clients.TimeEntryRowViewModel.ModeLabel(m), ((int)m).ToString(), m == CommunicationMode))
            .ToList();
    }

    /// <summary>Validates business rules and returns the entity to save. Adds model errors on failure.</summary>
    public async Task<TimeEntryCreateSaveData> PrepareDataForSave(ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary modelState, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var client = await clients.GetByIdAsync(ClientId, ct);
        if (client.ProviderId != provider.Id) throw new EntityNotFoundException("Client not found.");
        ClientName = client.FullName;

        var nowUtc = DateTimeOffset.UtcNow;
        var today = EasternTime.Today(nowUtc);
        if (Date.HasValue)
        {
            if (Date.Value > today) modelState.AddModelError(nameof(Date), ProviderDashboardMessages.ValidationFutureDate);
            if (Date.Value < RtmPeriods.CalendarMonthOf(today).Start.AddMonths(-1)) modelState.AddModelError(nameof(Date), ProviderDashboardMessages.ValidationOldDate);
        }
        if (Activity == RtmActivityType.InteractiveCommunication)
        {
            if (CommunicationMode == CommunicationMode.None) modelState.AddModelError(nameof(CommunicationMode), ProviderDashboardMessages.ValidationModeRequired);
            else if (CommunicationMode == CommunicationMode.SecureMessage) modelState.AddModelError(nameof(CommunicationMode), ProviderDashboardMessages.ValidationModeNotLive);
        }

        if (!modelState.IsValid)
        {
            BuildOptions();
            return null;
        }

        var startedAt = EasternTime.AtLocal(Date!.Value, StartTime!.Value);
        return new TimeEntryCreateSaveData
        {
            Entry = new RtmTimeEntry
            {
                ProviderId = provider.Id,
                ClientId = client.Id,
                StartedAt = startedAt,
                ActiveSeconds = Minutes!.Value * 60,
                Activity = Activity!.Value,
                Source = RtmTimeSource.Manual,
                CommunicationMode = Activity == RtmActivityType.InteractiveCommunication ? CommunicationMode : CommunicationMode.None,
                Note = Note.Trim(),
                LastHeartbeatAt = startedAt,
                IsAttested = true,
                AttestedAt = nowUtc
            }
        };
    }
}
