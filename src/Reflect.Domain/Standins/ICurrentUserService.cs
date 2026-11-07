// STAND-IN: exact contract from the Reflect XR tech spec (Part 2, "Backend conventions").
// Delete this file on merge; the real Reflect.Domain interface takes over.
namespace Reflect.Domain.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }          // the local user id (not the Entra object id)
    string EntraObjectId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
