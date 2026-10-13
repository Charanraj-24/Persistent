using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Standins;

namespace Reflect.Web.Controllers;

public class HomeController : Controller
{
    // The root redirects signed-in users to their role's dashboard.
    public IActionResult Index() =>
        User.IsInRole(DemoUsers.ProviderRole)
            ? RedirectToAction("Index", "Dashboard", new { area = "Provider" })
            : RedirectToAction("AccessDenied", "Account");

    public IActionResult Error() => View("~/Views/Shared/Error.cshtml");

    [Route("Home/Status/{code:int}")]
    public IActionResult Status(int code) =>
        code == 404 ? View("~/Views/Shared/NotFound.cshtml") : View("~/Views/Shared/Error.cshtml");
}
