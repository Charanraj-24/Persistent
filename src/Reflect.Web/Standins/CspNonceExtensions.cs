using System.Security.Cryptography;

namespace Reflect.Web.Standins;

// STAND-IN: the real app provides GetCspNonce(). Same name and behavior: one random value per request, kept in HttpContext.Items.
public static class CspNonceExtensions
{
    private const string NonceKey = "__CspNonce";

    public static string GetCspNonce(this HttpContext context)
    {
        if (context.Items.TryGetValue(NonceKey, out var existing) && existing is string nonce) return nonce;
        nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items[NonceKey] = nonce;
        return nonce;
    }
}

// STAND-IN: sends the same Content-Security-Policy as the real app (tech spec, Part 2), so anything that would break after merge breaks here first.
public class ContentSecurityPolicyMiddleware
{
    public const string Policy =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self' data:; connect-src 'self' wss:; " +
        "frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self';";

    private readonly RequestDelegate _next;

    public ContentSecurityPolicyMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Content-Security-Policy"] = Policy;
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });
        return _next(context);
    }
}
