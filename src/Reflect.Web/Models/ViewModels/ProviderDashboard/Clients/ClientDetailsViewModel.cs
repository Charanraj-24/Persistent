using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

/// <summary>
/// The client page shell: header, RTM status strip and review-time tracker. Tab content is loaded
/// separately (one request per tab, on first open) by <c>ClientsController.Tab</c>.
/// </summary>
public class ClientDetailsViewModel
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Mrn { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string DiagnosisLabel { get; set; } = string.Empty;
    public bool IsPaused { get; set; }

    public RtmEvaluation Rtm { get; set; }
    public BadgeViewModel RtmBadge { get; set; }
    public string NextStep { get; set; } = string.Empty;
    public string DevicePeriodLabel { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public int DataDaysPercent { get; set; }
    public int MinutesPercent { get; set; }
    public ReimbursementEstimate Estimate { get; set; } = new(0, 0);

    public string TrackingKey { get; set; } = string.Empty;

    /// <summary>Tabs in display order: key, label, icon.</summary>
    public static readonly IReadOnlyList<(string Key, string Label, string Icon)> Tabs = new[]
    {
        ("overview", "Overview", "ri-eye-line"),
        ("engagement", "Engagement", "ri-bar-chart-2-line"),
        ("response", "Therapeutic response", "ri-heart-pulse-line"),
        ("art", "Artwork", "ri-palette-line"),
        ("notes", "Notes", "ri-sticky-note-line"),
        ("physiologic", "Physiologic (preview)", "ri-pulse-line"),
        ("rtm", "RTM & billing", "ri-money-dollar-circle-line")
    };

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessionsRepo, IRtmTimeEntryRepository timeEntriesRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var client = scope.Client;
        var period = RtmPeriods.DevicePeriodFor(client.RtmStartDate, scope.Today);
        var month = RtmPeriods.CalendarMonthOf(scope.Today);

        // Only what the status strip needs: this period's sessions and this month's time.
        var sessions = await sessionsRepo.GetForClientsAsync(scope.UserIds, EasternTime.StartOfDayUtc(period.Start), scope.NowUtc, ct);
        var entries = await timeEntriesRepo.GetForClientAsync(client.Id, EasternTime.StartOfDayUtc(month.Start), scope.NowUtc, ct);

        ClientId = client.Id;
        Name = client.FullName;
        Initials = client.Initials;
        Age = scope.Today.Year - client.DateOfBirth.Year - (scope.Today < client.DateOfBirth.AddYears(scope.Today.Year - client.DateOfBirth.Year) ? 1 : 0);
        Mrn = client.Mrn;
        DiagnosisCode = client.PrimaryDiagnosisCode;
        DiagnosisLabel = client.PrimaryDiagnosisLabel;
        IsPaused = client.Status == ClientRtmStatus.Paused;

        Rtm = RtmClientEvaluator.Evaluate(client, sessions, entries, scope.Today, EasternTime.ToDate);
        RtmBadge = RtmStatusPresenter.StatusBadge(Rtm);
        Estimate = RtmFeeSchedule.Estimate(Rtm.Codes);
        NextStep = RtmStatusPresenter.NextStep(Rtm);
        DataDaysPercent = RtmStatusPresenter.DataDaysPercent(Rtm);
        MinutesPercent = RtmStatusPresenter.MinutesPercent(Rtm);
        DevicePeriodLabel = $"{period.Start.ToUsDateString()} to {period.End.ToUsDateString()}";
        MonthLabel = scope.Today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        TrackingKey = Guid.NewGuid().ToString("N");
    }
}
