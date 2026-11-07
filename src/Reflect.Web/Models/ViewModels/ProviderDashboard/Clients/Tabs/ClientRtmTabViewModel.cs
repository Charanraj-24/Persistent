using System.Globalization;
using System.Text.Json;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Messages;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients.Tabs;

/// <summary>RTM &amp; billing tab: thresholds, codes, completed periods, data-day calendar, time log and attestation.</summary>
public class ClientRtmTabViewModel
{
    public Reflect.Web.Models.ViewModels.ProviderDashboard.Audit.ClientAccessHistoryViewModel AccessHistory { get; set; }
    public Guid ClientId { get; set; }
    public RtmEvaluation Rtm { get; set; }
    public BadgeViewModel RtmBadge { get; set; }
    public string DevicePeriodLabel { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public List<DayCellViewModel> DayGrid { get; set; } = new();
    public List<CompletedPeriodViewModel> CompletedPeriods { get; set; } = new();
    public List<TimeEntryRowViewModel> TimeEntries { get; set; } = new();

    public string ConfirmAttestTitle { get; set; } = ProviderDashboardMessages.ConfirmAttestTitle;
    public string ConfirmAttestText { get; set; } = string.Empty;
    public string ConfirmDiscardTitle { get; set; } = ProviderDashboardMessages.ConfirmDiscardTitle;
    public string ConfirmDiscardText { get; set; } = ProviderDashboardMessages.ConfirmDiscardText;

    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(Guid clientId, ICurrentUserService currentUser, IProviderProfileRepository providers,
        IProviderClientRepository clients, IAppSessionRepository sessionsRepo, IRtmTimeEntryRepository timeEntriesRepo, CancellationToken ct)
    {
        var scope = await ClientScope.LoadAsync(clientId, currentUser, providers, clients, ct);
        var client = scope.Client;
        var today = scope.Today;
        var period = RtmPeriods.DevicePeriodFor(client.RtmStartDate, today);
        var month = RtmPeriods.CalendarMonthOf(today);

        var sessions = await sessionsRepo.GetForClientsAsync(scope.UserIds, scope.DaysAgoUtc(65), scope.NowUtc, ct);
        var entries = await timeEntriesRepo.GetForClientAsync(client.Id, EasternTime.StartOfDayUtc(month.Start), scope.NowUtc, ct);

        ClientId = client.Id;
        Rtm = RtmClientEvaluator.Evaluate(client, sessions, entries, today, EasternTime.ToDate);
        RtmBadge = RtmStatusPresenter.StatusBadge(Rtm);
        DevicePeriodLabel = $"{period.Start.ToUsDateString()} to {period.End.ToUsDateString()}";
        MonthLabel = today.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        ConfirmAttestText = ProviderDashboardMessages.ConfirmAttestText(Rtm.PendingMinutes);

        CompletedPeriods = RtmClientEvaluator.CompletedPeriods(client, sessions, new DateRange(today.AddDays(-60), today), today, EasternTime.ToDate)
            .OrderByDescending(p => p.Period.End)
            .Select(CompletedPeriodViewModel.FromDomain)
            .ToList();

        var dataDays = sessions.Select(s => EasternTime.ToDate(s.StartedAt)).ToHashSet();
        for (var d = period.Start; d <= period.End; d = d.AddDays(1))
        {
            DayGrid.Add(new DayCellViewModel { Label = d.ToUsDateString(), Day = d.Day, HasData = dataDays.Contains(d), IsFuture = d > today, IsToday = d == today });
        }

        TimeEntries = entries.OrderByDescending(e => e.StartedAt).Select(TimeEntryRowViewModel.FromEntity).ToList();

        // Cumulative attested minutes through the month, against the 10/20/40 thresholds.
        var days = Enumerable.Range(0, today.Day).Select(i => month.Start.AddDays(i)).ToList();
        var attested = entries.Where(e => e.IsAttested).ToList();
        double running = 0;
        var cumulative = days.Select(d =>
        {
            running += attested.Where(e => EasternTime.ToDate(e.StartedAt) == d).Sum(e => e.Minutes);
            return Math.Floor(running);
        }).ToList();

        ChartDataJson = JsonSerializer.Serialize(new
        {
            minutes = new
            {
                labels = days.Select(d => d.ToString("M/d", CultureInfo.InvariantCulture)),
                cumulative,
                thresholds = new[] { RtmBillingRules.MinMinutesShortManagement, RtmBillingRules.MinMinutesFullManagement, RtmBillingRules.MinMinutesFullManagement + RtmBillingRules.AdditionalBlockMinutes }
            }
        });
    }
}
