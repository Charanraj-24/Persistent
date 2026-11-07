using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Audit;

public class AuditRowViewModel
{
    public string When { get; set; } = string.Empty;
    public long WhenSort { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Record { get; set; } = "-";
    public string Detail { get; set; } = string.Empty;
    public bool Succeeded { get; set; }

    public static AuditRowViewModel From(AccessAuditEntry e) => new()
    {
        When = e.OccurredAt.ToUsDateTimeString(),
        WhenSort = e.OccurredAt.ToUnixTimeSeconds(),
        Actor = e.ActorName,
        Role = e.ActorRole.Replace('_', ' '),
        Action = AuditLabels.For(e.Action),
        Icon = AuditLabels.Icon(e.Action),
        Record = AuditLabels.RecordRef(e.ClientId),
        Detail = e.Detail,
        Succeeded = e.Succeeded
    };
}
