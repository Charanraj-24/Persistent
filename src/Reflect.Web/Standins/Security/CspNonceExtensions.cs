// STAND-IN: per-request CSP nonce and the real app's Content-Security-Policy header. Delete on merge.
using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Http;

public static class CspNonceExtensions
{
    private const string ItemKey = "__csp_nonce";

    public static string GetCspNonce(this HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var existing) && existing is string s) return s;
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[ItemKey] = nonce;
        return nonce;
    }
}

public static class CspMiddlewareExtensions
{
    /// <summary>
    /// Sends the exact policy from the tech spec. With <c>Csp:StrictScripts = true</c> it sends the upcoming
    /// stricter rule (script-src 'self' 'nonce-...'), so anything that would break after the change breaks now.
    /// </summary>
    public static IApplicationBuilder UseReflectCsp(this IApplicationBuilder app, bool strictScripts)
    {
        return app.Use(async (context, next) =>
        {
            var nonce = context.GetCspNonce();
            var scriptSrc = strictScripts ? $"script-src 'self' 'nonce-{nonce}'" : "script-src 'self' 'unsafe-inline'";
            context.Response.Headers["Content-Security-Policy"] =
                $"default-src 'self'; {scriptSrc}; style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data:; font-src 'self' data:; connect-src 'self' wss:; " +
                "frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self';";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
    }
}
