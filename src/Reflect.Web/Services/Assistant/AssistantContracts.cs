namespace Reflect.Web.Services.Assistant;

/// <summary>Settings for the clinical assistant (section "Assistant" in configuration).</summary>
public class AssistantOptions
{
    /// <summary>Any OpenAI-compatible chat-completions endpoint. Default: Groq.</summary>
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    /// <summary>Groq retired its Llama models for free/developer tiers on 8/16/2026; gpt-oss-20b is Groq's recommended replacement.</summary>
    public string Model { get; set; } = "openai/gpt-oss-20b";
    /// <summary>Never commit this. Set with user-secrets or the Assistant__ApiKey environment variable.</summary>
    public string ApiKey { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 900;
    public double Temperature { get; set; } = 0.2;
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

public sealed record AssistantTurn(string Role, string Content);

/// <summary>What is sent to the model: instructions, de-identified data, prior turns, and the new question.</summary>
public sealed class AssistantPrompt
{
    public string SystemPrompt { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public IReadOnlyList<AssistantTurn> History { get; init; } = Array.Empty<AssistantTurn>();
    public string Question { get; init; } = string.Empty;
}

public sealed class AssistantReply
{
    public string Answer { get; init; } = string.Empty;
    public bool IsOffline { get; init; }
    public bool IsError { get; init; }
    public string Model { get; init; } = string.Empty;
}

public interface IClinicalAssistant
{
    Task<AssistantReply> AskAsync(AssistantPrompt prompt, CancellationToken ct);
}
