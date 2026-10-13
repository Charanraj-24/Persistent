using Reflect.Domain.ProviderDashboard.Enums;
using Reflect.Domain.ProviderDashboard.Rules;
using Reflect.Web.Infrastructure;
using Reflect.Web.Models.ViewModels.Provider.Patients;
using Reflect.Web.Models.ViewModels.Provider.Shared;

namespace Reflect.Web.Models.ViewModels.Provider.Communications;

public class CommunicationIndexViewModel
{
    public BillingPeriod Period { get; set; }
    public bool IsCurrentPeriod { get; set; }
    public IReadOnlyList<PeriodOption> PeriodOptions { get; set; } = Array.Empty<PeriodOption>();
    public int TotalCount { get; set; }
    public int InteractiveCount { get; set; }
    public int TotalMinutes { get; set; }
    public int EnrolledPatients { get; set; }
    public int EnrolledWithInteractive { get; set; }
    public List<CommunicationRowViewModel> Rows { get; set; } = new();
    public List<PatientOption> MissingInteractive { get; set; } = new();

    public async Task PrepareData(ProviderDashboardRepositories repos, ProviderContext context, string periodKey, CancellationToken ct)
    {
        Period = Shared.PeriodOptions.Resolve(periodKey, context.Today);
        IsCurrentPeriod = Period.Contains(context.Today);
        PeriodOptions = Shared.PeriodOptions.List(Period, context.Today);

        var patients = await repos.PatientRepo.GetByProviderAsync(context.ProviderId, ct);
        var names = patients.ToDictionary(p => p.Id, p => p.FullName);
        var comms = await repos.CareCommunicationRepo.GetByPatientsAsync(names.Keys.ToList(), Period.StartUtc, Period.EndUtcExclusive, ct);

        Rows = comms.OrderByDescending(c => c.OccurredAt).Select(c =>
        {
            var row = CommunicationRowViewModel.FromEntity(c);
            row.PatientName = names[c.PatientId];
            return row;
        }).ToList();

        TotalCount = comms.Count;
        InteractiveCount = comms.Count(c => c.IsInteractive);
        TotalMinutes = comms.Sum(c => c.DurationMinutes);

        var enrolled = patients.Where(p => p.Status == PatientStatus.Active && p.RtmConsentOn.HasValue && p.RtmConsentOn <= Period.End).ToList();
        var withInteractive = comms.Where(c => c.IsInteractive).Select(c => c.PatientId).ToHashSet();
        EnrolledPatients = enrolled.Count;
        EnrolledWithInteractive = enrolled.Count(p => withInteractive.Contains(p.Id));
        MissingInteractive = Shared.PatientOptions.From(enrolled.Where(p => !withInteractive.Contains(p.Id)));
    }
}
