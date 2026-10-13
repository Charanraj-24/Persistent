using System.Text.Json;

namespace Reflect.Web.Standins;

public sealed class SidebarMenuItem
{
    public bool IsCategory { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

// STAND-IN: renders the sidebar from the same flat JSON format the real app uses. On merge, menus/provider-menu.json
// is copied into the real sidebar menus and this class is deleted.
public static class SidebarMenuProvider
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };
    private static List<SidebarMenuItem> _cache;

    public static IReadOnlyList<SidebarMenuItem> Load(IWebHostEnvironment env)
    {
        if (_cache != null && !env.IsDevelopment()) return _cache;
        var path = Path.Combine(env.ContentRootPath, "menus", "provider-menu.json");
        _cache = JsonSerializer.Deserialize<List<SidebarMenuItem>>(File.ReadAllText(path), Options) ?? new List<SidebarMenuItem>();
        return _cache;
    }

    public static bool IsActive(SidebarMenuItem item, string currentPath)
    {
        if (string.IsNullOrEmpty(item.Url) || string.IsNullOrEmpty(currentPath)) return false;
        var section = item.Url.EndsWith("/Index", StringComparison.OrdinalIgnoreCase) ? item.Url[..^"/Index".Length] : item.Url;
        return currentPath.Equals(section, StringComparison.OrdinalIgnoreCase)
            || currentPath.StartsWith(section + "/", StringComparison.OrdinalIgnoreCase);
    }
}
