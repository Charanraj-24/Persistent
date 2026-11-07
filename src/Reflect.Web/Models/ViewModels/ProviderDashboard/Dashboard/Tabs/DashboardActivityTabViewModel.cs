using Reflect.Domain.Entities.ProviderDashboard;
using Reflect.Domain.Helpers;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Domain.ProviderDashboard;
using Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Dashboard.Tabs;

/// <summary>Dashboard tab: the latest Reflect sessions across the caseload.</summary>
public class DashboardActivityTabViewModel
{
    public List<ActivityRowViewModel> Rows { get; set; } = new();

    public async Task PrepareData(ICurrentUserService currentUser, IProviderProfileRepository providers, IProviderClientRepository clientsRepo,
        IAppSessionRepository sessionsRepo, CancellationToken ct)
    {
        var provider = await CaseloadSnapshot.LoadProviderAsync(currentUser, providers, ct);
        var clients = await clientsRepo.GetByProviderAsync(provider.Id, ct);
        var byUser = clients.ToDictionary(c => c.ClientUserId);
        var now = DateTimeOffset.UtcNow;
        var sessions = await sessionsRepo.GetForClientsAsync(byUser.Keys.ToList(), now.AddDays(-7), now, ct);

        Rows = sessions.OrderByDescending(s => s.StartedAt).Take(25).Select(s =>
        {
            var c = byUser[s.ClientUserId];
            return new ActivityRowViewModel
            {
                ClientId = c.Id, Name = c.FullName, Initials = c.Initials,
                ModuleName = ModuleCatalog.Get(s.ModuleKey).Name,
                PlatformLabel = s.Platform == ReflectPlatform.ReflectXR ? "Reflect XR" : "Reflect Mobile",
                PlatformIcon = s.Platform == ReflectPlatform.ReflectXR ? "ri-goggles-line" : "ri-smartphone-line",
                When = s.StartedAt.ToUsDateTimeString(),
                Minutes = (int)Math.Round(s.DurationMinutes)
            };
        }).ToList();
    }
}
