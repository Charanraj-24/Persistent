namespace Reflect.Domain.Common;

// Matches the Reflect XR ICurrentUserService contract exactly. The demo implements it with a selectable test user.
public interface ICurrentUserService
{
    Guid? UserId { get; }          // the local user id (not the Entra object id)
    string EntraObjectId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
