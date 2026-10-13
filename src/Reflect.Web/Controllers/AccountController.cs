using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Messages;
using Reflect.Web.Standins;

namespace Reflect.Web.Controllers;

// STAND-IN: lets the demo switch between test users. The real app's shared sign-in area replaces it on merge.
public class AccountController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SwitchUser(Guid userId)
    {
        var user = DemoUsers.All.FirstOrDefault(u => u.UserId == userId);
        if (user == null) return BadRequest();

        Response.Cookies.Append(DemoUsers.CookieName, user.UserId.ToString(), new CookieOptions
        {
            HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps
        });
        TempData[TempDataKeys.Success] = ProviderDashboardMessages.SignedInAs(user.DisplayName);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
