using Microsoft.AspNetCore.Authentication.Cookies;
using Reflect.Domain.Interfaces;
using Reflect.Domain.Interfaces.Repositories;
using Reflect.Web.Data.InMemory;
using Reflect.Web.Services.Assistant;
using Reflect.Web.Services.Ingestion;
using Reflect.Web.Services.Simulation;
using Reflect.Web.Standins.Errors;
using Reflect.Web.Standins.Layout;
using Reflect.Web.Standins.Security;

var builder = WebApplication.CreateBuilder(args);

var mvc = builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<StandinExceptionFilter>(); // STAND-IN: real app maps EntityNotFoundException itself
});
builder.Services.AddSingleton<StandinMenuProvider>(); // STAND-IN: renders menus/provider-menu.json in the demo layout
#if RAZOR_RUNTIME_COMPILATION
mvc.AddRazorRuntimeCompilation();
#endif

// ---- STAND-IN: fake sign-in + current user (Entra External ID in the real app) ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<DemoSignInService>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/StandinAccount/Login";
        options.LogoutPath = "/StandinAccount/Logout";
        options.AccessDeniedPath = "/StandinAccount/AccessDenied";
        options.Cookie.Name = "reflect-demo-auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // idle timeout for a clinical app
        options.SlidingExpiration = true;
        options.Events.OnValidatePrincipal = ctx =>
        {
            // Background polling must not keep an idle session alive.
            if (ctx.Request.Path.StartsWithSegments("/Provider/Live/Feed") || ctx.Request.Path.StartsWithSegments("/Provider/Notifications/Count") || ctx.Request.Path.StartsWithSegments("/Provider/Live/CheckInStatus")) ctx.ShouldRenew = false;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToLogin = ctx =>
        {
            // AJAX calls (tabs, live feed, assistant) get a 401 instead of the login page's HTML.
            if (ctx.Request.Headers.XRequestedWith == "XMLHttpRequest" || ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Headers.XRequestedWith == "XMLHttpRequest" || ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            else
                ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ---- One named policy for the whole Provider area. On merge, point it at the real provider role. ----
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ProviderOnly", policy => policy.RequireRole(DemoUsers.ProviderRole));
    // Facility billing totals only. Deliberately separate: admins never get the clinical ProviderOnly screens.
    options.AddPolicy("FacilityAdminOnly", policy => policy.RequireRole(DemoUsers.FacilityAdminRole));
});
builder.Services.AddScoped<IFacilityAccessService, StandinFacilityAccessService>(); // STAND-IN

// ---- DEMO data: in-memory repositories seeded with synthetic data ----
var store = new InMemoryProviderDashboardStore();
SyntheticDataSeeder.Seed(store, DateTimeOffset.UtcNow);
builder.Services.AddSingleton(store);
builder.Services.AddScoped<IProviderProfileRepository, InMemoryProviderProfileRepository>();
builder.Services.AddScoped<IProviderClientRepository, InMemoryProviderClientRepository>();
builder.Services.AddScoped<IAppSessionRepository, InMemoryAppSessionRepository>();
builder.Services.AddScoped<IMoodCheckInRepository, InMemoryMoodCheckInRepository>();
builder.Services.AddScoped<IAssessmentResultRepository, InMemoryAssessmentResultRepository>();
builder.Services.AddScoped<IObservationRepository, InMemoryObservationRepository>();
builder.Services.AddScoped<IRtmTimeEntryRepository, InMemoryRtmTimeEntryRepository>();
builder.Services.AddScoped<IIngestEventRepository, InMemoryIngestEventRepository>();
builder.Services.AddScoped<IAccessAuditRepository, InMemoryAccessAuditRepository>();
builder.Services.AddScoped<Reflect.Web.Services.Audit.AuditLogger>();
builder.Services.AddSingleton<Reflect.Web.Services.Notifications.NotificationReadState>();
builder.Services.AddSingleton<Reflect.Web.Services.CheckIn.PhoneCheckInService>(); // DEMO: QR audience check-in
builder.Services.AddSingleton<Reflect.Web.Services.Assistant.AssistantChatStore>(); // DEMO: in-memory chat history

// ---- Live data: ingestion (API + simulator share IngestionService) ----
builder.Services.AddScoped<IngestionService>();
builder.Services.AddSingleton<LiveDemoState>();          // DEMO ONLY
builder.Services.AddHostedService<LiveDemoSimulator>();   // DEMO ONLY: idle until started from /Provider/Live

// ---- Clinical assistant: OpenAI-compatible API (Groq by default). Offline fallback when no key is set. ----
builder.Services.Configure<AssistantOptions>(builder.Configuration.GetSection("Assistant"));
var assistantOptions = builder.Configuration.GetSection("Assistant").Get<AssistantOptions>() ?? new AssistantOptions();
if (assistantOptions.IsConfigured)
    builder.Services.AddHttpClient<IClinicalAssistant, OpenAiCompatibleAssistant>();
else
    builder.Services.AddSingleton<IClinicalAssistant, OfflineClinicalAssistant>();

var app = builder.Build();

// Behind a reverse proxy or tunnel (Azure App Service, Cloudflare Tunnel, ngrok, dev tunnels): trust X-Forwarded-Proto/For
// so cookies are marked Secure and the QR code points at the public https URL. Off by default.
if (app.Configuration.GetValue("Hosting:BehindProxy", false))
{
    var fwd = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
    };
    fwd.KnownIPNetworks.Clear();
    fwd.KnownProxies.Clear();
    app.UseForwardedHeaders(fwd);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/StandinAccount/Error");
    app.UseHsts();
}

app.UseReflectCsp(app.Configuration.GetValue("Csp:StrictScripts", false));
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=StandinAccount}/{action=Root}/{id?}");

app.Run();
