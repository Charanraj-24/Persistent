using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Reflect.Web.Api.Models;
using Reflect.Web.Services.Ingestion;

namespace Reflect.Web.Api;

/// <summary>
/// Ingestion endpoints the Reflect apps (or Reflect.Api / a BigQuery export job) call to deliver client data.
/// Machine-to-machine: authenticated with the X-Reflect-Ingest-Key header, so no cookies and no anti-forgery token.
/// </summary>
[ApiController]
[Route("api/ingest")]
[AllowAnonymous]
[IngestApiKey]
[IgnoreAntiforgeryToken]
public class IngestController : ControllerBase
{
    private readonly IngestionService _ingestion;
    public IngestController(IngestionService ingestion) => _ingestion = ingestion;

    [HttpPost("sessions")]
    public async Task<IActionResult> Sessions([FromBody] SessionIngestRequest request, CancellationToken ct) =>
        ToResult(await _ingestion.RecordSessionAsync(request, "api", ct));

    [HttpPost("assessments")]
    public async Task<IActionResult> Assessments([FromBody] AssessmentIngestRequest request, CancellationToken ct) =>
        ToResult(await _ingestion.RecordAssessmentAsync(request, "api", ct));

    [HttpPost("observations")]
    public async Task<IActionResult> Observations([FromBody] ObservationIngestRequest request, CancellationToken ct) =>
        ToResult(await _ingestion.RecordObservationAsync(request, "api", ct));

    private IActionResult ToResult(IngestResult r) =>
        r.Ok ? Accepted(new { ok = true, eventId = r.EventId, summary = r.Summary }) : BadRequest(new { ok = false, errors = r.Errors });
}

/// <summary>Rejects requests without the configured ingestion key (constant-time comparison).</summary>
public sealed class IngestApiKeyAttribute : Attribute, IAsyncActionFilter
{
    public const string HeaderName = "X-Reflect-Ingest-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expected = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["Ingest:ApiKey"] ?? string.Empty;
        if (expected.Length < 16)
        {
            context.Result = new ObjectResult(new { ok = false, errors = new[] { "Ingestion is not configured." } }) { StatusCode = 503 };
            return;
        }
        var supplied = context.HttpContext.Request.Headers[HeaderName].ToString();
        var match = CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)), SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
        if (!match)
        {
            context.Result = new UnauthorizedObjectResult(new { ok = false, errors = new[] { "Missing or invalid ingestion key." } });
            return;
        }
        await next();
    }
}
