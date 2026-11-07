using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Reflect.Web.Services.Assistant;

/// <summary>
/// Calls an OpenAI-compatible chat-completions API (Groq by default) from the server. The browser never talks to
/// the AI provider (CSP blocks external hosts anyway), and the API key never reaches the browser.
/// </summary>
public class OpenAiCompatibleAssistant : IClinicalAssistant
{
    private readonly HttpClient _http;
    private readonly AssistantOptions _options;
    private readonly ILogger<OpenAiCompatibleAssistant> _log;

    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public OpenAiCompatibleAssistant(HttpClient http, IOptions<AssistantOptions> options, ILogger<OpenAiCompatibleAssistant> log)
    {
        _http = http;
        _options = options.Value;
        _log = log;
    }

    public async Task<AssistantReply> AskAsync(AssistantPrompt prompt, CancellationToken ct)
    {
        var messages = new List<object>
        {
            new { role = "system", content = prompt.SystemPrompt },
            new { role = "system", content = "<DATA>\n" + prompt.Context + "\n</DATA>" }
        };
        messages.AddRange(prompt.History.Select(t => new { role = t.Role, content = t.Content }));
        messages.Add(new { role = "user", content = prompt.Question });

        var body = JsonSerializer.Serialize(new
        {
            model = _options.Model,
            messages,
            temperature = _options.Temperature,
            max_tokens = _options.MaxTokens
        }, Json);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl.TrimEnd('/') + "/chat/completions")
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        try
        {
            using var response = await _http.SendAsync(request, timeout.Token);
            var text = await response.Content.ReadAsStringAsync(timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Assistant provider returned {Status}.", (int)response.StatusCode);
                return Error(response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => "The AI service rejected the API key. Check Assistant:ApiKey.",
                    HttpStatusCode.TooManyRequests => "The AI service is rate-limiting requests. Try again in a minute.",
                    HttpStatusCode.NotFound or HttpStatusCode.BadRequest when text.Contains("model", StringComparison.OrdinalIgnoreCase)
                        => $"The AI service doesn't offer the model \"{_options.Model}\". Update Assistant:Model.",
                    _ => "The AI service is unavailable right now. Try again shortly."
                });
            }

            using var doc = JsonDocument.Parse(text);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
            var model = doc.RootElement.TryGetProperty("model", out var m) ? m.GetString() ?? _options.Model : _options.Model;
            return new AssistantReply { Answer = content.Trim(), Model = model };
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Error("The AI service took too long to answer. Try a shorter question.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            _log.LogWarning(ex, "Assistant call failed.");
            return Error("The AI service could not be reached.");
        }
    }

    private AssistantReply Error(string message) => new() { Answer = message, IsError = true, Model = _options.Model };
}

/// <summary>Used when no API key is configured: says so, and shows the facts the AI would have been given.</summary>
public class OfflineClinicalAssistant : IClinicalAssistant
{
    public Task<AssistantReply> AskAsync(AssistantPrompt prompt, CancellationToken ct)
    {
        var summary = prompt.Context.Split('\n')
            .SkipWhile(l => !l.StartsWith("## Summary", StringComparison.Ordinal)).Skip(1)
            .TakeWhile(l => !l.StartsWith("## ", StringComparison.Ordinal))
            .Where(l => !string.IsNullOrWhiteSpace(l)).Take(12);
        var answer = "The AI model isn't connected yet (set Assistant:ApiKey), so I can't answer free-form questions. " +
                     "Here is the summary I would have used:\n" + string.Join("\n", summary);
        return Task.FromResult(new AssistantReply { Answer = answer, IsOffline = true, Model = "offline" });
    }
}
