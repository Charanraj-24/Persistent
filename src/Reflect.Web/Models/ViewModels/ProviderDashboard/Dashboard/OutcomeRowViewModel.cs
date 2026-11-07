using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.ProviderDashboard.Clinical;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard;

public class OutcomeRowViewModel
{
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phq { get; set; } = "-";
    public string Gad { get; set; } = "-";
    public int PhqChangeSort { get; set; }
    public int ActiveDays { get; set; }
    public BadgeViewModel Status { get; set; }

    private static string Fmt(ClientOutcome o) => o == null ? "-" : $"{o.Baseline} to {o.Latest} ({(o.Change > 0 ? "+" : "")}{o.Change})";

    public static OutcomeRowViewModel From(ProviderClient c, ClientOutcome phq, ClientOutcome gad, int activeDays)
    {
        var lead = phq ?? gad;
        var status = lead.IsRemission ? new BadgeViewModel("success", "Remission", "ri-award-line")
            : lead.IsResponse ? new BadgeViewModel("success", "Response", "ri-arrow-down-line")
            : lead.Improved ? new BadgeViewModel("info", "Improved", "ri-arrow-down-line")
            : lead.Worsened ? new BadgeViewModel("danger", "Worsened", "ri-arrow-up-line")
            : new BadgeViewModel("info", "Stable", "ri-subtract-line");
        return new OutcomeRowViewModel
        {
            ClientId = c.Id, Name = c.FullName, Phq = Fmt(phq), Gad = Fmt(gad),
            PhqChangeSort = phq?.Change ?? 99, ActiveDays = activeDays, Status = status
        };
    }
}
