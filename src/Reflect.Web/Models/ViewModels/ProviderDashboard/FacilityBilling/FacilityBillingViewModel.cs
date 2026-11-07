using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Reports;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.FacilityBilling;

/// <summary>
/// Facility Admin: RTM billing totals per provider for a month. Client data is used only to compute counts on the
/// server; no client names, identifiers or clinical values are put in the ViewModel (HIPAA minimum necessary).
/// </summary>
public class FacilityBillingViewModel
{
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public List<SelectListItem> FacilityOptions { get; set; } = new();
    public string Month { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public string AsOfLabel { get; set; } = string.Empty;
    public bool IsCurrentMonth { get; set; }
    public List<SelectListItem> MonthOptions { get; set; } = new();

    public List<ProviderBillingRowViewModel> Rows { get; set; } = new();
    public int TotalProviders => Rows.Count;
    public int TotalClients => Rows.Sum(r => r.ClientsOnRtm);
    public int TotalClientsWithCodes => Rows.Sum(r => r.ClientsWithCodes);
    public int TotalMinutes => Rows.Sum(r => r.AttestedMinutes);
    public int TotalGaps => Rows.Sum(r => r.DocumentationGaps);
    public Dictionary<string, int> CodeTotals { get; set; } = new();
    public ReimbursementEstimate Estimate => RtmFeeSchedule.Estimate(CodeTotals);
    public string ChartDataJson { get; set; } = "{}";

    public async Task PrepareData(string month, Guid? facilityId, ICurrentUserService currentUser, IFacilityAccessService facilityAccess,
        IProviderProfileRepository providers, IProviderClientRepository clients, IAppSessionRepository sessions,
        IRtmTimeEntryRepository timeEntries, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new EntityNotFoundException("No signed-in user.");
        var facilities = await facilityAccess.GetFacilitiesForAdminAsync(userId, ct);
        if (facilities.Count == 0) throw new EntityNotFoundException("You don't manage any facilities.");
        var facility = facilities.FirstOrDefault(f => f.Id == facilityId) ?? facilities[0];
        FacilityId = facility.Id;
        FacilityName = facility.Name;
        FacilityOptions = facilities.Select(f => new SelectListItem(f.Name, f.Id.ToString(), f.Id == facility.Id)).ToList();

        var nowUtc = DateTimeOffset.UtcNow;
        var realToday = EasternTime.Today(nowUtc);
        var monthStart = MonthlyReportViewModel.ParseMonth(month, realToday);
        var range = RtmPeriods.CalendarMonthOf(monthStart);
        var asOf = range.End < realToday ? range.End : realToday;
        var endUtc = asOf == realToday ? nowUtc : EasternTime.StartOfDayUtc(asOf.AddDays(1)).AddTicks(-1);
        var fromUtc = EasternTime.StartOfDayUtc(asOf.AddDays(-65));

        Month = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        MonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        AsOfLabel = asOf.ToUsDateString();
        IsCurrentMonth = asOf == realToday;
        for (var i = 0; i < 3; i++)
        {
            var m = RtmPeriods.CalendarMonthOf(realToday).Start.AddMonths(-i);
            var key = m.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            MonthOptions.Add(new SelectListItem(m.ToString("MMMM yyyy", CultureInfo.InvariantCulture), key, key == Month));
        }

        foreach (var p in await providers.GetByFacilityAsync(facility.Id, ct))
        {
            var list = (await clients.GetByProviderAsync(p.Id, ct))
                .Where(c => c.Status != ClientRtmStatus.Discharged && c.RtmStartDate <= range.End).ToList();
            var userIds = list.Select(c => c.ClientUserId).ToList();
            var s = await sessions.GetForClientsAsync(userIds, fromUtc, endUtc, ct);
            var entries = await timeEntries.GetForProviderAsync(p.Id, EasternTime.StartOfDayUtc(range.Start), endUtc, ct);

            var results = list.Select(c =>
            {
                var mine = s.Where(x => x.ClientUserId == c.ClientUserId).ToList();
                var eval = RtmClientEvaluator.Evaluate(c, mine, entries, asOf, EasternTime.ToDate);
                var completed = RtmClientEvaluator.CompletedPeriods(c, mine, range, realToday, EasternTime.ToDate);
                return new ClientMonthResult(eval, RtmClientEvaluator.CodesForMonth(eval, completed, c.RtmConsentDate.HasValue), c.RtmConsentDate.HasValue);
            });
            Rows.Add(ProviderBillingRowViewModel.FromDomain(p, ProviderRtmRollup.From(results)));
        }

        foreach (var kv in Rows.SelectMany(r => r.CodeUnits))
            CodeTotals[kv.Key] = CodeTotals.GetValueOrDefault(kv.Key) + kv.Value;
        CodeTotals = CodeTotals.OrderBy(k => k.Key).ToDictionary(k => k.Key, k => k.Value);

        var codes = CodeTotals.Keys.ToList();
        ChartDataJson = JsonSerializer.Serialize(new
        {
            providers = Rows.Select(r => r.Name),
            minutes = Rows.Select(r => r.AttestedMinutes),
            pending = Rows.Select(r => r.PendingMinutes),
            codes = codes.Select(code => new { code, units = Rows.Select(r => r.CodeUnits.GetValueOrDefault(code)) })
        });
    }

    public string ToCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Provider,Credentials,NPI,Estimated Medicare amount,Clients on RTM,Clients with codes,Attested minutes,Pending minutes,Clients with live interaction,Missing live interaction,Missing consent,Pending time,Codes (units)");
        foreach (var r in Rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                r.Name, r.Credentials, r.Npi, r.Estimate.Formatted, r.ClientsOnRtm.ToString(CultureInfo.InvariantCulture), r.ClientsWithCodes.ToString(CultureInfo.InvariantCulture),
                r.AttestedMinutes.ToString(CultureInfo.InvariantCulture), r.PendingMinutes.ToString(CultureInfo.InvariantCulture),
                r.ClientsWithLiveInteraction.ToString(CultureInfo.InvariantCulture), r.ClientsMissingLiveInteraction.ToString(CultureInfo.InvariantCulture),
                r.ClientsMissingConsent.ToString(CultureInfo.InvariantCulture), r.ClientsWithPendingTime.ToString(CultureInfo.InvariantCulture), r.Codes
            }.Select(v => "\"" + (v.Length > 0 && "=+-@".Contains(v[0]) ? "'" + v : v).Replace("\"", "\"\"") + "\"")));
        }
        return sb.ToString();
    }
}
