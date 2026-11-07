namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class ClientActivityItemViewModel
{
    public DateTimeOffset At { get; set; }
    public string When { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string SourceLabel { get; set; } = string.Empty;
    /// <summary>Arrived through ingestion in the last 30 minutes.</summary>
    public bool IsNew { get; set; }
    public bool IsUrgent { get; set; }
    public string Key { get; set; } = string.Empty;
}
