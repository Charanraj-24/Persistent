namespace Reflect.Domain.Interfaces;

public sealed record FacilityRef(Guid Id, string Name);

/// <summary>
/// Which facilities the signed-in Facility Admin manages. The real app already knows this (an admin can manage
/// several facilities and switches between them); the demo backs it with a stand-in.
/// </summary>
public interface IFacilityAccessService
{
    Task<List<FacilityRef>> GetFacilitiesForAdminAsync(Guid userId, CancellationToken ct);
}
