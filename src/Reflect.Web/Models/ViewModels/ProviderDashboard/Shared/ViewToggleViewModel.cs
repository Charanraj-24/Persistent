namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

/// <summary>Model for the _ViewToggle partial. Panels are marked with data-view-panel="group:key".</summary>
public class ViewToggleViewModel
{
    public string Group { get; set; } = string.Empty;
    public string AriaLabel { get; set; } = "View";
    public List<ViewToggleOption> Options { get; set; } = new();

    public static ViewToggleViewModel Of(string group, params (string Key, string Label, string Icon)[] options) => new()
    {
        Group = group,
        Options = options.Select(o => new ViewToggleOption { Key = o.Key, Label = o.Label, Icon = o.Icon }).ToList()
    };
}
