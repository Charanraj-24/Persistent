using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class TimeEntryRowViewModel
{
    public Guid Id { get; set; }
    public string When { get; set; } = string.Empty;
    public long WhenSort { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string SourceLabel { get; set; } = string.Empty;
    public string SourceIcon { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public int Seconds { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool IsAttested { get; set; }
    public bool IsLiveInteraction { get; set; }

    public static TimeEntryRowViewModel FromEntity(RtmTimeEntry e) => new()
    {
        Id = e.Id,
        When = e.StartedAt.ToUsDateTimeString(),
        WhenSort = e.StartedAt.ToUnixTimeSeconds(),
        Activity = ActivityLabel(e.Activity),
        SourceLabel = e.Source == RtmTimeSource.AutoTracked ? "Auto-tracked" : "Manual",
        SourceIcon = e.Source == RtmTimeSource.AutoTracked ? "ri-timer-line" : "ri-edit-line",
        Mode = ModeLabel(e.CommunicationMode),
        Duration = $"{e.ActiveSeconds / 60}:{e.ActiveSeconds % 60:00}",
        Seconds = e.ActiveSeconds,
        Note = e.Note,
        IsAttested = e.IsAttested,
        IsLiveInteraction = e.IsRealTimeInteraction
    };

    public static string ActivityLabel(RtmActivityType a) => a switch
    {
        RtmActivityType.DataReview => "Data review",
        RtmActivityType.CarePlanUpdate => "Care plan update",
        RtmActivityType.InteractiveCommunication => "Live interaction",
        RtmActivityType.CareCoordination => "Care coordination",
        _ => a.ToString()
    };

    public static string ModeLabel(CommunicationMode m) => m switch
    {
        CommunicationMode.Video => "Video",
        CommunicationMode.Phone => "Phone",
        CommunicationMode.InPerson => "In person",
        CommunicationMode.SecureMessage => "Secure message",
        _ => "-"
    };
}
