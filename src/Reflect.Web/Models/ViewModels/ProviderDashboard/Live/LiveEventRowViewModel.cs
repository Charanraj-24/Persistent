using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Live;

public class LiveEventRowViewModel
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsUrgent { get; set; }
    public string When { get; set; } = string.Empty;
    public long At { get; set; }

    public static LiveEventRowViewModel FromEntity(IngestEvent e, ProviderClient c) => new()
    {
        ClientId = c.Id,
        Name = c.FullName,
        Initials = c.Initials,
        Kind = e.Kind,
        Summary = e.Summary,
        Source = e.Source switch { "phone" => "audience phone", "api" => "Reflect app (API)", _ => e.Source },
        IsUrgent = e.IsUrgent,
        When = e.ReceivedAt.ToUsDateTimeString(),
        At = e.ReceivedAt.ToUnixTimeMilliseconds()
    };
}
