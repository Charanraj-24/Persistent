using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Web.Services.Audit;

/// <summary>
/// Records an access audit entry after the action runs. The client is read from an argument named "id" or "clientId",
/// or from a bound model's ClientId property. <paramref name="detailArg"/> names an argument whose value is logged as detail (for example a tab name).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AuditAccessAttribute : Attribute, IAsyncActionFilter
{
    private readonly AuditAction _action;
    private readonly string _detailArg;

    public AuditAccessAttribute(AuditAction action, string detailArg = null)
    {
        _action = action;
        _detailArg = detailArg;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var clientId = FindClientId(context.ActionArguments);
        var detail = _detailArg != null && context.ActionArguments.TryGetValue(_detailArg, out var d) ? d?.ToString() ?? string.Empty : string.Empty;
        var executed = await next();

        var status = executed.Result switch
        {
            IStatusCodeActionResult s when s.StatusCode.HasValue => s.StatusCode.Value,
            _ => context.HttpContext.Response.StatusCode
        };
        var succeeded = executed.Exception == null && status < 400;
        var logger = context.HttpContext.RequestServices.GetRequiredService<AuditLogger>();
        await logger.LogAsync(_action, clientId, detail, succeeded, context.HttpContext.RequestAborted);
    }

    private static Guid? FindClientId(IDictionary<string, object> args)
    {
        foreach (var name in new[] { "id", "clientId" })
            if (args.TryGetValue(name, out var v) && v is Guid g && g != Guid.Empty) return g;
        foreach (var v in args.Values)
        {
            var prop = v?.GetType().GetProperty("ClientId");
            if (prop?.GetValue(v) is Guid g && g != Guid.Empty) return g;
        }
        return null;
    }
}
