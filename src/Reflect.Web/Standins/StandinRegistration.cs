using Microsoft.AspNetCore.Authentication;
using Reflect.Domain.Common;

namespace Reflect.Web.Standins;

// STAND-IN wiring. On merge, the real app's authentication and ICurrentUserService replace all of this.
public static class StandinRegistration
{
    public static IServiceCollection AddReflectStandins(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddAuthentication(DemoAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(DemoAuthenticationHandler.SchemeName, _ => { });
        services.AddScoped<ICurrentUserService, DemoCurrentUserService>();
        return services;
    }
}
