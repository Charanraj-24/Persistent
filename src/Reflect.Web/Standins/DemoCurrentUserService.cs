using System.Security.Claims;
using Reflect.Domain.Common;

namespace Reflect.Web.Standins;

// STAND-IN: implements the real ICurrentUserService contract from the demo sign-in claims.
public class DemoCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public DemoCurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal User => _accessor.HttpContext?.User;

    public Guid? UserId => Guid.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string EntraObjectId => User?.FindFirstValue("oid") ?? string.Empty;

    public string Email => User?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}
