using Reflect.Domain.Entities.ProviderDashboard;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Notes;

public static class NoteTopics
{
    public static string Label(NoteTopic t) => t switch
    {
        NoteTopic.Session => "Session",
        NoteTopic.Assessment => "Questionnaire",
        NoteTopic.Artwork => "Artwork",
        NoteTopic.Billing => "RTM and billing",
        NoteTopic.Safety => "Safety",
        _ => "General"
    };

    public static string Icon(NoteTopic t) => t switch
    {
        NoteTopic.Session => "ri-play-circle-line",
        NoteTopic.Assessment => "ri-survey-line",
        NoteTopic.Artwork => "ri-palette-line",
        NoteTopic.Billing => "ri-money-dollar-circle-line",
        NoteTopic.Safety => "ri-shield-cross-line",
        _ => "ri-sticky-note-line"
    };

    /// <summary>Topics a provider can pick in the general "Add note" form (attached topics come from the item clicked).</summary>
    public static readonly NoteTopic[] Pickable = { NoteTopic.General, NoteTopic.Safety, NoteTopic.Billing };
}
