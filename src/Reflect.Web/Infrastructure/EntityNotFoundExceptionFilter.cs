using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Reflect.Domain.Common;

namespace Reflect.Web.Infrastructure;

// Maps EntityNotFoundException to a 404 page (MVC) or a JSON error body (AJAX), like the real app.
public class EntityNotFoundExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not EntityNotFoundException ex) return;

        var isAjax = context.HttpContext.Request.Headers.XRequestedWith == "XMLHttpRequest";
        context.Result = isAjax
            ? new NotFoundObjectResult(new { success = false, message = ex.Message })
            : new ViewResult { ViewName = "~/Views/Shared/NotFound.cshtml", StatusCode = StatusCodes.Status404NotFound };
        context.ExceptionHandled = true;
    }
}
