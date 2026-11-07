// STAND-IN: demo sign-in with test accounts and a cookie session. Delete on merge: the real app signs in with
// Microsoft Entra External ID. Kept deliberately close to production behavior: generic error messages,
// lockout after repeated failures, constant-time password check, short idle timeout.
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Reflect.Web.Standins.Security;

public enum SignInOutcome { Success, InvalidCredentials, LockedOut }

public class DemoSignInService
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset Since)> _failures = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _password;

    public DemoSignInService(IConfiguration config) => _password = config["Demo:Password"] ?? "reflect-demo";

    public string DemoPassword => _password;

    public DemoUser Validate(string email, string password, out SignInOutcome outcome)
    {
        email = (email ?? string.Empty).Trim();
        if (_failures.TryGetValue(email, out var f) && f.Count >= MaxFailures && DateTimeOffset.UtcNow - f.Since < LockoutWindow)
        {
            outcome = SignInOutcome.LockedOut;
            return null;
        }

        var user = DemoUsers.All.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        var ok = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(password ?? string.Empty)), SHA256.HashData(Encoding.UTF8.GetBytes(_password)));

        if (user == null || !ok)
        {
            _failures.AddOrUpdate(email, _ => (1, DateTimeOffset.UtcNow),
                (_, old) => DateTimeOffset.UtcNow - old.Since > LockoutWindow ? (1, DateTimeOffset.UtcNow) : (old.Count + 1, old.Since));
            outcome = SignInOutcome.InvalidCredentials;
            return null;
        }

        _failures.TryRemove(email, out _);
        outcome = SignInOutcome.Success;
        return user;
    }

    public static Task SignInAsync(HttpContext http, DemoUser user)
    {
        var claims = new List<Claim>
        {
            new("local_user_id", user.UserId.ToString()),
            new("oid", user.EntraObjectId),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        return http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = true });
    }
}
