// STAND-IN: the demo Facility Admin manages the one synthetic facility. Delete on merge; the real app knows
// which facilities each admin manages.
using Reflect.Domain.Interfaces;
using Reflect.Web.Data.InMemory;

namespace Reflect.Web.Standins.Security;

public class StandinFacilityAccessService : IFacilityAccessService
{
    public Task<List<FacilityRef>> GetFacilitiesForAdminAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult(userId == DemoUsers.FacilityAdmin.UserId
            ? new List<FacilityRef> { new(SyntheticDataSeeder.FacilityId, SyntheticDataSeeder.FacilityName) }
            : new List<FacilityRef>());
}
