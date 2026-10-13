using Reflect.Domain.ProviderDashboard.Entities;
using Reflect.Domain.ProviderDashboard.Enums;

namespace Reflect.Web.Models.ViewModels.Provider.Shared;

public sealed record PatientOption(Guid Id, string Label);

public static class PatientOptions
{
    public static List<PatientOption> From(IEnumerable<Patient> patients) =>
        patients
            .Where(p => p.Status != PatientStatus.Discharged)
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new PatientOption(p.Id, $"{p.LastName}, {p.FirstName} ({p.ChartNumber})"))
            .ToList();
}
