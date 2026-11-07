// STAND-IN: maps EntityNotFoundException to a not-found page (MVC) or a JSON error body (AJAX),
// like the real app does. Delete on merge.
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Reflect.Domain.Exceptions;

namespace Reflect.Web.Standins.Errors;

public class StandinExceptionFilter : IExceptionFilter
{
    private readonly IModelMetadataProvider _metadata;
    public StandinExceptionFilter(IModelMetadataProvider metadata) => _metadata = metadata;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not EntityNotFoundException ex) return;

        var isAjax = context.HttpContext.Request.Headers.XRequestedWith == "XMLHttpRequest";
        if (isAjax)
        {
            context.Result = new NotFoundObjectResult(new { ok = false, error = ex.Message });
        }
        else
        {
            var viewData = new ViewDataDictionary(_metadata, context.ModelState) { ["Title"] = "Not found", ["Message"] = ex.Message };
            context.Result = new ViewResult { ViewName = "~/Standins/Views/NotFound.cshtml", ViewData = viewData, StatusCode = 404 };
        }
        context.ExceptionHandled = true;
    }
}
