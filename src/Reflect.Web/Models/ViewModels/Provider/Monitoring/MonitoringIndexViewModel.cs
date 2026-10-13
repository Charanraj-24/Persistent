using Reflect.Domain.Common;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Patients;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Monitoring;

/// <summary>The provider's own record of monitoring time, for reconciling against what is billed.</summary>
public class MonitoringIndexViewModel
{
    public BillingPeriod Period { get; set; }
    public IReadOnlyList<PeriodOption> PeriodOptions { get; set; } = Array.Empty<PeriodOption>();
    public int TotalSeconds { get; set; }
    public int AutoSeconds { get; set; }
    public int ManualSeconds { get; set; }
    public int PatientsReviewed { get; set; }
    public int EntryCount { get; set; }
    public List<MonitoringPatientTotalViewModel> PatientTotals { get; set; } = new();
    public List<MonitoringEntryRowViewModel> Entries { get; set; } = new();
    public List<int> DailyMinutes { get; set; } = new();
    public List<string> DailyLabels { get; set; } = new();

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, string periodKey, CancellationToken ct)
    {
        Period = Shared.PeriodOptions.Resolve(periodKey, context.Today);
        PeriodOptions = Shared.PeriodOptions.List(Period, context.Today);

        var patients = (await repos.PatientRepo.GetByProviderAsync(context.ProviderId, ct)).ToDictionary(p => p.Id);
        var entries = await repos.MonitoringTimeEntryRepo.GetByProviderAsync(context.ProviderId, Period.StartUtc, Period.EndUtcExclusive, ct);
        entries = entries.Where(e => patients.ContainsKey(e.PatientId) && e.ActiveSeconds > 0).ToList();

        TotalSeconds = entries.Sum(e => e.ActiveSeconds);
        AutoSeconds = entries.Where(e => e.Source == Domain.ProviderDashboard.Enums.MonitoringTimeSource.AutoTracked).Sum(e => e.ActiveSeconds);
        ManualSeconds = TotalSeconds - AutoSeconds;
        PatientsReviewed = entries.Select(e => e.PatientId).Distinct().Count();
        EntryCount = entries.Count;

        PatientTotals = entries
            .GroupBy(e => e.PatientId)
            .Select(g => new MonitoringPatientTotalViewModel
            {
                PatientId = g.Key,
                PatientName = patients[g.Key].FullName,
                Initials = patients[g.Key].Initials,
                Entries = g.Count(),
                Seconds = g.Sum(e => e.ActiveSeconds),
                LastReviewed = g.Max(e => e.StartedAt).ToUsDateString()
            })
            .OrderByDescending(t => t.Seconds)
            .ToList();

        Entries = entries.OrderByDescending(e => e.StartedAt).Select(e =>
        {
            var row = MonitoringEntryRowViewModel.FromEntity(e);
            row.PatientName = patients[e.PatientId].FullName;
            return row;
        }).ToList();

        var lastDay = Period.Contains(context.Today) ? context.Today : Period.End;
        var byDay = entries.GroupBy(e => e.StartedAt.ToEasternDate()).ToDictionary(g => g.Key, g => g.Sum(e => e.ActiveSeconds));
        for (var d = Period.Start; d <= lastDay; d = d.AddDays(1))
        {
            DailyLabels.Add(d.ToString("M/d", System.Globalization.CultureInfo.InvariantCulture));
            DailyMinutes.Add((byDay.TryGetValue(d, out var s) ? s : 0) / 60);
        }
    }

    public string ToCsv()
    {
        var csv = new CsvBuilder().Row("Started (Eastern)", "Patient", "Source", "Active minutes", "Active seconds", "Note");
        foreach (var e in Entries)
        {
            csv.Row(e.When, e.PatientName, e.Source.Text, e.Seconds / 60, e.Seconds, e.Note);
        }
        return csv.ToString();
    }
}

public class MonitoringPatientTotalViewModel
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public int Entries { get; set; }
    public int Seconds { get; set; }
    public string LastReviewed { get; set; } = string.Empty;
    public int Minutes => Seconds / 60;
}
