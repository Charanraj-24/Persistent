using Reflect.Domain.ProviderDashboard.Notifications;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notifications;

public class NotificationItemViewModel
{
    public string Key { get; set; } = string.Empty;
    public NotificationKind Kind { get; set; }
    public Guid ClientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string Tone => Kind switch
    {
        NotificationKind.Urgent => "danger",
        NotificationKind.Clinical => "warning",
        NotificationKind.Attestation => "info",
        _ => "success"
    };
}
