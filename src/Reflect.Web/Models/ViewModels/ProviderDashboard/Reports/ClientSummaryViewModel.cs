using System.Globalization;
using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Exceptions;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Domain.ProviderDashboard.Rtm;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Reports;

/// <summary>Printable RTM documentation for one client and one month (supports the claim and the chart note).</summary>
public class ClientSummaryViewModel
{
    public Guid ClientId { get; set; }
    public string Month { get; set; } = string.Empty;
    public string MonthLabel { get; set; } = string.Empty;
    public string AsOfLabel { get; set; } = string.Empty;
    public string GeneratedAt { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string Payer { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string ConsentDate { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderNpi { get; set; } = string.Empty;
    public string FacilityName { get; set; } = string.Empty;

    public string DevicePeriod { get; set; } = string.Empty;
    public List<string> DataDayList { get; set; } = new();
    public RtmEvaluation Rtm { get; set; }
    public List<CompletedPeriodViewModel> CompletedPeriods { get; set; } = new();
    /// <summary>Codes this month's documentation supports (completed device periods + treatment management).</summary>
    public List<BillableCode> Codes { get; set; } = new();
    public EngagementSummary Summary { get; set; }
    public string TopModule { get; set; } = string.Empty;
    public List<AssessmentRowViewModel> Assessments { get; set; } = new();
    public List<TimeEntryRowViewModel> TimeEntries { get; set; } = new();
    public List<ClinicalFlag> Flags { get; set; } = new();

    public async Task PrepareData(Guid clientId, string month, ICurrentUserService currentUser,
        IProviderProfileRepository providers, IProviderClientRepository clients, IAppSessionRepository sessions,
        IMoodCheckInRepository checkIns, IAssessmentResultRepository assessments, IRtmTimeEntryRepository timeEntries, CancellationToken ct)
    {
        var realToday = EasternTime.Today(DateTimeOffset.UtcNow);
        var monthStart = MonthlyReportViewModel.ParseMonth(month, realToday);
        var range = RtmPeriods.CalendarMonthOf(monthStart);
        var asOf = range.End < realToday ? range.End : realToday;

        var snap = await CaseloadSnapshot.LoadAsync(currentUser, providers, clients, sessions, checkIns, assessments, timeEntries, asOf, ct);
        var client = snap.Clients.FirstOrDefault(c => c.Id == clientId) ?? throw new EntityNotFoundException("Client not found.");

        ClientId = client.Id;
        Month = monthStart.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        MonthLabel = monthStart.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
        AsOfLabel = asOf.ToUsDateString();
        GeneratedAt = DateTimeOffset.UtcNow.ToUsDateTimeString();
        ClientName = client.FullName;
        DateOfBirth = client.DateOfBirth.ToUsDateString();
        Mrn = client.Mrn;
        Payer = client.PayerName;
        Diagnosis = $"{client.PrimaryDiagnosisCode} {client.PrimaryDiagnosisLabel}";
        ConsentDate = client.RtmConsentDate.HasValue ? client.RtmConsentDate.Value.ToUsDateString() : "Not on file";
        ProviderName = $"{snap.Provider.FullName}, {snap.Provider.Credentials}";
        ProviderNpi = snap.Provider.Npi;
        FacilityName = snap.Provider.FacilityName;

        var period = RtmPeriods.DevicePeriodFor(client.RtmStartDate, asOf);
        DevicePeriod = $"{period.Start.ToUsDateString()} to {period.End.ToUsDateString()}";
        DataDayList = snap.SessionsFor(client).Select(s => EasternTime.ToDate(s.StartedAt))
            .Where(d => period.Contains(d) && d <= asOf).Distinct().OrderBy(d => d)
            .Select(d => d.ToString("M/d", CultureInfo.InvariantCulture)).ToList();
        Rtm = snap.EvaluateRtm(client);
        var completed = RtmClientEvaluator.CompletedPeriods(client, snap.SessionsFor(client), range, realToday, EasternTime.ToDate);
        CompletedPeriods = completed.Select(CompletedPeriodViewModel.FromDomain).ToList();
        Codes = RtmClientEvaluator.CodesForMonth(Rtm, completed, client.RtmConsentDate.HasValue);
        Summary = snap.Summarize(client);
        TopModule = string.IsNullOrEmpty(Summary.TopModuleKey) ? "None" : ModuleCatalog.Get(Summary.TopModuleKey).Name;
        Flags = snap.FlagsFor(client);

        AssessmentResult prevPhq = null, prevGad = null;
        foreach (var a in snap.AssessmentsFor(client).OrderBy(a => a.CompletedAt))
        {
            var prev = a.Instrument == AssessmentInstrument.Phq9 ? prevPhq : prevGad;
            if (range.Contains(EasternTime.ToDate(a.CompletedAt))) Assessments.Add(AssessmentRowViewModel.FromEntity(a, prev));
            if (a.Instrument == AssessmentInstrument.Phq9) prevPhq = a; else prevGad = a;
        }

        TimeEntries = snap.TimeEntriesFor(client)
            .Where(e => range.Contains(EasternTime.ToDate(e.StartedAt)))
            .OrderBy(e => e.StartedAt)
            .Select(TimeEntryRowViewModel.FromEntity).ToList();
    }
}
