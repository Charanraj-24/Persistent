using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Messages;

namespace Reflect.Web.Models.ViewModels.Provider.Monitoring;

public class MonitoringTrackingResult
{
    public bool Success { get; set; } = true;
    public Guid EntryId { get; set; }
    public int EntrySeconds { get; set; }
    /// <summary>All monitoring seconds for this patient in the current billing period, including this entry.</summary>
    public int PeriodSeconds { get; set; }
    public int CreditedSeconds { get; set; }
    public int HeartbeatIntervalSeconds { get; set; } = MonitoringTimePolicy.HeartbeatIntervalSeconds;
}

/// <summary>AJAX start/heartbeat for auto-tracked monitoring time. Builds the entity to save; the controller persists it.</summary>
public class MonitoringTrackingViewModel
{
    public MonitoringTrackingResult Result { get; private set; } = new();

    /// <summary>The client starts an entry after a few seconds of activity; those seconds are credited on the first heartbeat.</summary>
    public const int ClientStartDelaySeconds = 5;

    public async Task<MonitoringSaveData> PrepareStart(ProviderDashboardRepositories repos, ProviderContext context, Guid patientId, CancellationToken ct)
    {
        var patient = await repos.PatientRepo.GetForProviderAsync(patientId, context.ProviderId, ct)
            ?? throw new EntityNotFoundException(ProviderDashboardMessages.PatientNotFound);

        var entry = new MonitoringTimeEntry
        {
            PatientId = patient.Id,
            ProviderId = context.ProviderId,
            Source = MonitoringTimeSource.AutoTracked,
            StartedAt = context.NowUtc.AddSeconds(-ClientStartDelaySeconds),
            LastActivityAt = context.NowUtc.AddSeconds(-ClientStartDelaySeconds),
            DateCreated = context.NowUtc
        };
        Result = new MonitoringTrackingResult
        {
            EntryId = entry.Id,
            PeriodSeconds = await PeriodSeconds(repos, context, patient.Id, ct)
        };
        return new MonitoringSaveData { Entry = entry };
    }

    public async Task<MonitoringSaveData> PrepareHeartbeat(ProviderDashboardRepositories repos, ProviderContext context, Guid entryId, int activeSeconds, bool close, CancellationToken ct)
    {
        var entry = await repos.MonitoringTimeEntryRepo.GetByIdAsync(entryId, ct);
        if (entry == null || entry.ProviderId != context.ProviderId || entry.Source != MonitoringTimeSource.AutoTracked)
            throw new EntityNotFoundException(ProviderDashboardMessages.MonitoringEntryNotFound);

        var credited = MonitoringTimePolicy.ApplyHeartbeat(entry, activeSeconds, context.NowUtc);
        if (close) entry.IsClosed = true;

        Result = new MonitoringTrackingResult
        {
            EntryId = entry.Id,
            EntrySeconds = entry.ActiveSeconds,
            CreditedSeconds = credited,
            PeriodSeconds = await PeriodSeconds(repos, context, entry.PatientId, ct)
        };
        return new MonitoringSaveData { Entry = entry };
    }

    private static async Task<int> PeriodSeconds(ProviderDashboardRepositories repos, ProviderContext context, Guid patientId, CancellationToken ct)
    {
        var period = BillingPeriod.Containing(context.Today);
        var entries = await repos.MonitoringTimeEntryRepo.GetByPatientAsync(patientId, period.StartUtc, period.EndUtcExclusive, ct);
        return entries.Sum(e => e.ActiveSeconds);
    }
}
