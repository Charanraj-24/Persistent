using Reflect.Domain.Common;

namespace Reflect.Domain.ProviderDashboard.Entities;

/// <summary>
/// One physiologic measurement (Remote Physiologic Monitoring preview). Kept generic on purpose:
/// a new metric is a new <see cref="Catalogs.PhysiologicMetricCatalog"/> entry, not a new table.
/// </summary>
public class PhysiologicReading : BaseEntity
{
    public Guid PatientId { get; set; }
    public Guid? ActivitySessionId { get; set; }
    public string MetricCode { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
    /// <summary>When in the session it was taken: "start", "end", or empty for a standalone reading.</summary>
    public string Context { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
}
