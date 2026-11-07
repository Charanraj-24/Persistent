using Reflect.Domain.ProviderDashboard.Assistant;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Assistant;

/// <summary>Model for the _AssistantPanel partial (chat drawer) on the dashboard and client pages.</summary>
public class AssistantPanelViewModel
{
    public Guid? ClientId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
    public bool IsConfigured { get; set; }
    public string ModelName { get; set; } = string.Empty;

    public static AssistantPanelViewModel ForClient(Guid clientId, string clientName, bool configured, string model) => new()
    {
        ClientId = clientId, Title = "Ask about " + clientName,
        Subtitle = "Answers come only from this client's Reflect data.",
        Suggestions = AssistantPrompts.ClientSuggestions, IsConfigured = configured, ModelName = model
    };

    public static AssistantPanelViewModel ForCaseload(bool configured, string model) => new()
    {
        Title = "Ask about your caseload",
        Subtitle = "Answers come only from your clients' Reflect data.",
        Suggestions = AssistantPrompts.CaseloadSuggestions, IsConfigured = configured, ModelName = model
    };
}
