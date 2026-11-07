// STAND-IN: signs every request in as the selected demo user. Delete on merge.
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Reflect.Web.Standins.Security;

public class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DemoAuth";
    public const string CookieName = "reflect-demo-user";

    public DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var key = Request.Cookies.TryGetValue(CookieName, out var value) ? value : string.Empty;
        var user = DemoUsers.Find(key);

        var claims = new List<Claim>
        {
            new("local_user_id", user.UserId.ToString()),
            new("oid", user.EntraObjectId),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.Redirect("/StandinAccount/AccessDenied");
        return Task.CompletedTask;
    }
}
