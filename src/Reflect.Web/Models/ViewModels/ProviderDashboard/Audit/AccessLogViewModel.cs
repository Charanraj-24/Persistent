using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Interfaces.Repositories;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Audit;

/// <summary>Facility Admin: access audit trail for the last 7 days. Who, what and when; no client names or clinical content.</summary>
public class AccessLogViewModel
{
    public const int Days = 7;
    public List<AuditRowViewModel> Rows { get; set; } = new();
    public int TotalEvents { get; set; }
    public int RecordsOpened { get; set; }
    public int ActiveUsers { get; set; }
    public int FailedSignIns { get; set; }
    public int Exports { get; set; }

    public async Task PrepareData(IAccessAuditRepository audit, CancellationToken ct)
    {
        var entries = await audit.GetRecentAsync(DateTimeOffset.UtcNow.AddDays(-Days), 2000, ct);
        TotalEvents = entries.Count;
        RecordsOpened = entries.Where(e => e.ClientId.HasValue).Select(e => e.ClientId).Distinct().Count();
        ActiveUsers = entries.Where(e => e.ActorUserId.HasValue).Select(e => e.ActorUserId).Distinct().Count();
        FailedSignIns = entries.Count(e => e.Action == AuditAction.SignInFailed);
        Exports = entries.Count(e => e.Action is AuditAction.ExportReport or AuditAction.PrintRtmDocumentation);
        Rows = entries.Select(AuditRowViewModel.From).ToList();
    }
}
