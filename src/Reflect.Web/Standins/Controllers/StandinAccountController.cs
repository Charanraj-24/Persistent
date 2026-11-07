// STAND-IN: sign-in portal, sign-out, test-user switcher and Access denied page for the isolated demo.
// Delete on merge (the shared sign-in Area and Entra External ID take over).
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Standins.Security;

namespace Reflect.Web.Standins.Controllers;

[AllowAnonymous]
public class StandinAccountController : Controller
{
    private readonly DemoSignInService _signIn;
    private readonly Reflect.Web.Services.Audit.AuditLogger _audit;

    public StandinAccountController(DemoSignInService signIn, Reflect.Web.Services.Audit.AuditLogger audit)
    {
        _signIn = signIn;
        _audit = audit;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl, bool timedOut = false)
    {
        if (User.Identity?.IsAuthenticated == true) return Redirect(HomeForCurrentUser());
        return View("~/Standins/Views/Login.cshtml", new LoginViewModel { ReturnUrl = returnUrl ?? string.Empty, TimedOut = timedOut, DemoPassword = _signIn.DemoPassword });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        vm.DemoPassword = _signIn.DemoPassword;
        var user = _signIn.Validate(vm.Email, vm.Password, out var outcome);
        if (user == null)
        {
            await _audit.LogActorAsync(Reflect.Domain.Entities.ProviderDashboard.AuditAction.SignInFailed, null, (vm.Email ?? string.Empty).Trim(), string.Empty, null,
                outcome == SignInOutcome.LockedOut ? "Locked out" : "Wrong email or password", false, HttpContext.RequestAborted);
            vm.Password = string.Empty;
            vm.Error = outcome == SignInOutcome.LockedOut
                ? "Too many attempts. Try again in a few minutes."
                : "The email or password is incorrect.";
            return View("~/Standins/Views/Login.cshtml", vm);
        }

        await DemoSignInService.SignInAsync(HttpContext, user);
        await _audit.LogActorAsync(Reflect.Domain.Entities.ProviderDashboard.AuditAction.SignIn, user.UserId, user.DisplayName, user.Role, null, string.Empty, true, HttpContext.RequestAborted);
        return Redirect(!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl) ? vm.ReturnUrl : DemoUserRouting.HomeFor(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync(Reflect.Domain.Entities.ProviderDashboard.AuditAction.SignOut, ct: HttpContext.RequestAborted);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Demo convenience in the profile menu: sign in as another test account in one click.</summary>
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchUser(string userKey)
    {
        var user = DemoUsers.All.FirstOrDefault(u => u.EntraObjectId == userKey);
        if (user == null) return RedirectToAction(nameof(Login));
        await DemoSignInService.SignInAsync(HttpContext, user);
        await _audit.LogActorAsync(Reflect.Domain.Entities.ProviderDashboard.AuditAction.SignIn, user.UserId, user.DisplayName, user.Role, null, "Switched test user", true, HttpContext.RequestAborted);
        TempData["ToastMessage"] = $"Signed in as {user.DisplayName}.";
        TempData["ToastType"] = "info";
        return Redirect(DemoUserRouting.HomeFor(user));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View("~/Standins/Views/Error.cshtml");

    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access denied";
        return View("~/Standins/Views/AccessDenied.cshtml");
    }

    public IActionResult Root() => Redirect(User.Identity?.IsAuthenticated == true ? HomeForCurrentUser() : "/StandinAccount/Login");

    private string HomeForCurrentUser() =>
        User.IsInRole(DemoUsers.FacilityAdminRole) ? "/Provider/FacilityBilling/Index"
        : User.IsInRole(DemoUsers.ProviderRole) ? "/Provider/Home/Dashboard"
        : "/StandinAccount/AccessDenied";
}

public class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public bool TimedOut { get; set; }
    public string DemoPassword { get; set; } = string.Empty;
}
