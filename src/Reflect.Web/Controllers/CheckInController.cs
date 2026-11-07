using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reflect.Web.Models.ViewModels.CheckIn;
using Reflect.Web.Services.CheckIn;
using Reflect.Web.Services.Ingestion;

namespace Reflect.Web.Controllers;

/// <summary>
/// DEMO: public phone check-in for the live presentation (scan the QR on the Live page).
/// Anonymous by design; protected by a short-lived join code, a per-code cap, per-device rate limiting and anti-forgery.
/// Not part of the production dashboard: in production this data comes from Reflect Mobile itself.
/// </summary>
[AllowAnonymous]
[Route("checkin")]
public class CheckInController : Controller
{
    private const string DeviceCookie = "rpd_device";
    private readonly PhoneCheckInService _service;
    private readonly IngestionService _ingestion;

    public CheckInController(PhoneCheckInService service, IngestionService ingestion)
    {
        _service = service;
        _ingestion = ingestion;
    }

    [HttpGet("")]
    public IActionResult Index(string code)
    {
        var vm = new PhoneCheckInViewModel { Code = (code ?? string.Empty).Trim().ToUpperInvariant() };
        vm.PrepareData(_service, DateTimeOffset.UtcNow);
        if (!string.IsNullOrEmpty(vm.Code) && !vm.CodeValid) vm.Error = "That code isn't active. Check it and try again.";
        return View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(PhoneCheckInViewModel vm, CancellationToken ct)
    {
        vm.Code = (vm.Code ?? string.Empty).Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;
        if (!ModelState.IsValid)
        {
            vm.PrepareData(_service, now);
            vm.Error = "Please complete the form.";
            return View("Index", vm);
        }
        var error = await vm.PrepareDataForSave(_service, _ingestion, DeviceKey(), now, ct);
        if (error != null)
        {
            vm.PrepareData(_service, now);
            vm.Error = error;
            return View("Index", vm);
        }
        return RedirectToAction(nameof(Thanks), new { code = vm.Code, lift = vm.MoodAfter - vm.MoodBefore });
    }

    [HttpGet("thanks")]
    public IActionResult Thanks(string code, int lift)
    {
        ViewData["Code"] = (code ?? string.Empty).Length == PhoneCheckInService.CodeLength ? code.ToUpperInvariant() : string.Empty;
        ViewData["Lift"] = Math.Clamp(lift, -9, 9);
        return View();
    }

    private string DeviceKey()
    {
        if (Request.Cookies.TryGetValue(DeviceCookie, out var id) && Guid.TryParse(id, out _)) return id;
        id = Guid.NewGuid().ToString("N");
        Response.Cookies.Append(DeviceCookie, id, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax, Secure = Request.IsHttps, MaxAge = TimeSpan.FromHours(12) });
        return id;
    }
}
