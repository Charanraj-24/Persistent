using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Reports;

/// <summary>Month-end RTM billing summary across the caseload, with CSV export.</summary>
public class MonthlyReportViewModel
{
    public string Month { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public string AsOfLabel { get; set; } = string.Empty;
    public bool IsCurrentMonth { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderNpi { get; set; } = string.Empty;
    public List<SelectListItem> MonthOptions { get; set; } = new();
    public List<MonthlyReportRowViewModel> Rows { get; set; } = new();

    public int ClientsBillable { get; set; }
    public int TotalMinutes { get; set; }
    public Dictionary<string, int> CodeTotals { get; set; } = new();
    public string ChartDataJson { get; set; } = "{}";
    public ReimbursementEstimate Estimate { get; set; } = new(0, 0);

    public static DateOnly ParseMonth(string month, DateOnly today)
    {
        if (!string.IsNullOrEmpty(month) &&
            DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) &&
            parsed <= today && parsed >= RtmPeriods.CalendarMonthOf(today).Start.AddMonths(-2))
            return parsed;
        return RtmPeriods.CalendarMonthOf(today).Start;
    }

    public async Task PrepareData(string month, ICurrentUserService currentUser,
        IProviderProfileRepository providers, IProviderClientRepository clients, IAppSessionRepository sessions,
        IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries, CancellationToken ct)
    {
        var realToday = EasternTime.Today(DateTimeOffset.UtcNow);
        var monthStart = ParseMonth(month, realToday);
        var range = RtmPeriods.CalendarMonthOf(monthStart);
        var asOf = range.End < realToday ? range.End : realToday;

        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, asOf, ct);

        Month = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        MonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        IsCurrentMonth = asOf == realToday;
        AsOfLabel = asOf.ToUsDateString();
        ProviderName = $"{snap.Provider.FullName}, {snap.Provider.Credentials}";
        ProviderNpi = snap.Provider.Npi;

        for (var i = 0; i < 3; i++)
        {
            var m = RtmPeriods.CalendarMonthOf(realToday).Start.AddMonths(-i);
            var key = m.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            MonthOptions.Add(new SelectListItem(m.ToString("MMMM yyyy", CultureInfo.InvariantCulture), key, key == Month));
        }

        foreach (var c in snap.Clients.Where(c => c.RtmStartDate <= range.End && c.Status != ClientRtmStatus.Discharged))
        {
            var eval = snap.EvaluateRtm(c);
            var entries = snap.TimeEntriesFor(c).Where(e => range.Contains(EasternTime.ToDate(e.StartedAt))).ToList();
            var completed = RtmClientEvaluator.CompletedPeriods(c, snap.SessionsFor(c), range, realToday, EasternTime.ToDate);
            var codes = RtmClientEvaluator.CodesForMonth(eval, completed, c.RtmConsentDate.HasValue);
            var live = entries.Where(e => e.IsAttested && e.IsRealTimeInteraction).OrderBy(e => e.StartedAt).FirstOrDefault();
            Rows.Add(new MonthlyReportRowViewModel
            {
                ClientId = c.Id,
                Name = c.FullName,
                SortName = $"{c.LastName}, {c.FirstName}",
                Mrn = c.Mrn,
                Payer = c.PayerName,
                DiagnosisCode = c.PrimaryDiagnosisCode,
                DevicePeriod = $"{RtmPeriods.DevicePeriodFor(c.RtmStartDate, asOf).Start.ToUsDateString()} to {RtmPeriods.DevicePeriodFor(c.RtmStartDate, asOf).End.ToUsDateString()}",
                DataDays = eval.DataDaysInPeriod,
                Minutes = eval.ManagementMinutes,
                PendingMinutes = eval.PendingMinutes,
                LiveInteraction = live == null ? "None" : $"{live.StartedAt.ToUsDateString()} ({Clients.TimeEntryRowViewModel.ModeLabel(live.CommunicationMode)})",
                DeviceSupply = completed.Count == 0
                    ? $"In progress: {eval.DataDaysInPeriod} {(eval.DataDaysInPeriod == 1 ? "day" : "days")} so far"
                    : string.Join("; ", completed.Select(p => $"{p.Period.Start.ToString("M/d", CultureInfo.InvariantCulture)}-{p.Period.End.ToString("M/d", CultureInfo.InvariantCulture)}: {p.DataDays} {(p.DataDays == 1 ? "day" : "days")}{(string.IsNullOrEmpty(p.Code) ? "" : $", {p.Code}")}")),
                Codes = codes.Count == 0 ? "None" : string.Join(", ", codes.Select(x => x.Units > 1 ? $"{x.Code} x{x.Units}" : x.Code)),
                CodeList = codes,
                Status = codes.Count > 0 && eval.Blockers.Count == 0 ? new BadgeViewModel("success", "Ready to bill", "ri-checkbox-circle-line")
                    : codes.Count > 0 ? new BadgeViewModel("warning", "Partly ready", "ri-error-warning-line")
                    : eval.Blockers.Count > 0 ? new BadgeViewModel("danger", "Action needed", "ri-close-circle-line")
                    : new BadgeViewModel("info", IsCurrentMonth ? "In progress" : "Not billable", "ri-loader-2-line"),
                Issues = string.Join(" ", eval.Blockers)
            });
            if (codes.Count > 0) ClientsBillable++;
            TotalMinutes += eval.ManagementMinutes;
            foreach (var code in codes)
                CodeTotals[code.Code] = CodeTotals.GetValueOrDefault(code.Code) + code.Units;
        }
        Rows = Rows.OrderBy(r => r.SortName).ToList();
        CodeTotals = CodeTotals.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);
        Estimate = RtmFeeSchedule.Estimate(CodeTotals);

        var byMinutes = Rows.OrderByDescending(r => r.Minutes).ToList();
        ChartDataJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            codes = new { labels = CodeTotals.Keys, units = CodeTotals.Values, descriptions = CodeTotals.Keys.Select(RtmBillingRules.Describe) },
            minutes = new { labels = byMinutes.Select(r => r.Name), values = byMinutes.Select(r => r.Minutes), pending = byMinutes.Select(r => r.PendingMinutes) },
            status = new
            {
                labels = Rows.GroupBy(r => r.Status.Text).Select(g => g.Key),
                counts = Rows.GroupBy(r => r.Status.Text).Select(g => g.Count())
            }
        });
    }

    public string ToCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Client,MRN,Payer,Diagnosis,Current device period,Current period data days,Completed device periods,Attested minutes,Pending minutes,Live interaction,Supported codes,Issues");
        foreach (var r in Rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                r.SortName, r.Mrn, r.Payer, r.DiagnosisCode, r.DevicePeriod,
                r.DataDays.ToString(CultureInfo.InvariantCulture), r.DeviceSupply, r.Minutes.ToString(CultureInfo.InvariantCulture),
                r.PendingMinutes.ToString(CultureInfo.InvariantCulture), r.LiveInteraction, r.Codes, r.Issues
            }.Select(Csv)));
        }
        return sb.ToString();
    }

    /// <summary>Quotes a CSV field and neutralizes spreadsheet formula injection.</summary>
    private static string Csv(string value)
    {
        value ??= string.Empty;
        if (value.Length > 0 && "=+-@".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
