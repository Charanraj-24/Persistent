// STAND-IN: implements the spec's ICurrentUserService over the demo claims. Delete on merge.
using System.Security.Claims;
using Reflect.Domain.Interfaces;

namespace Reflect.Web.Standins.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal User => _accessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue("local_user_id"), out var id) ? id : null;
    public string EntraObjectId => User?.FindFirstValue("oid") ?? string.Empty;
    public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;
}
