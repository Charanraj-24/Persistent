using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Reflect.Web.Standins;

public sealed record DemoUser(Guid UserId, string DisplayName, string Email, string Role, string Description);

/// <summary>
/// STAND-IN for Microsoft Entra External ID. Signs every request in as a selectable test user (cookie "rpd_demo_user").
/// Deleted on merge; the real app's sign-in takes over and the ProviderOnly policy is pointed at the real role.
/// </summary>
public static class DemoUsers
{
    public const string CookieName = "rpd_demo_user";
    public const string ProviderRole = "Provider";

    public static readonly IReadOnlyList<DemoUser> All = new List<DemoUser>
    {
        new(Guid.Parse("8b0f4d1e-2a6c-4c1e-9a51-0d7f1f3a1001"), "Dr. Maya Rivera, PsyD", "maya.rivera@example.test", ProviderRole, "Licensed psychologist, 16 patients"),
        new(Guid.Parse("8b0f4d1e-2a6c-4c1e-9a51-0d7f1f3a1002"), "Jordan Okafor, LCSW", "jordan.okafor@example.test", ProviderRole, "Clinical social worker, 6 patients"),
        new(Guid.Parse("8b0f4d1e-2a6c-4c1e-9a51-0d7f1f3a1003"), "Sam Taylor (End User)", "sam.taylor@example.test", "End_User", "No provider role: shows Access denied")
    };

    public static DemoUser Resolve(string cookieValue) =>
        All.FirstOrDefault(u => u.UserId.ToString() == cookieValue) ?? All[0];
}

public class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DemoAuth";

    public DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var user = DemoUsers.Resolve(Request.Cookies[DemoUsers.CookieName]);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new("oid", "demo-" + user.UserId.ToString("N")),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/Account/AccessDenied");
        return Task.CompletedTask;
    }
}
