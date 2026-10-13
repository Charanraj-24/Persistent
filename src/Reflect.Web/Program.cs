using System.Globalization;
using Reflect.Web.Infrastructure.InMemory;
using Reflect.Web.Infrastructure;
using Reflect.Web.Standins;

// US-only product: every request uses en-US formatting.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        options.Filters.Add<EntityNotFoundExceptionFilter>();
    })
    .AddRazorRuntimeCompilation();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddReflectStandins();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ProviderDashboardPolicies.ProviderOnly, policy => policy.RequireRole(DemoUsers.ProviderRole));
});
builder.Services.AddProviderDashboardInMemoryData();
builder.Services.AddScoped<ProviderContext>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseMiddleware<ContentSecurityPolicyMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("areas", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");

// Warm the synthetic data so the first page load is fast.
app.Services.GetRequiredService<InMemoryProviderDashboardStore>();

app.Run();
