// STAND-IN: reads the sidebar JSON (real menu format) so the demo layout can render it. Delete on merge;
// copy menus/provider-menu.json into the real sidebar menus instead.
using System.Text.Json;

namespace Reflect.Web.Standins.Layout;

public sealed class StandinMenuItem
{
    public bool IsCategory { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class StandinMenuProvider
{
    private readonly Lazy<List<StandinMenuItem>> _provider;
    private readonly Lazy<List<StandinMenuItem>> _facilityAdmin;

    public StandinMenuProvider(IWebHostEnvironment env)
    {
        _provider = new Lazy<List<StandinMenuItem>>(() => Load(env, "provider-menu.json"));
        _facilityAdmin = new Lazy<List<StandinMenuItem>>(() => Load(env, "facility-admin-menu.json"));
    }

    private static List<StandinMenuItem> Load(IWebHostEnvironment env, string file)
    {
        var json = File.ReadAllText(Path.Combine(env.ContentRootPath, "menus", file));
        return JsonSerializer.Deserialize<List<StandinMenuItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
    }

    public IReadOnlyList<StandinMenuItem> Items => _provider.Value;
    public IReadOnlyList<StandinMenuItem> FacilityAdminItems => _facilityAdmin.Value;

    public static bool IsActive(StandinMenuItem item, string path)
    {
        if (item.IsCategory || string.IsNullOrEmpty(item.Url)) return false;
        var prefix = item.Url[..item.Url.LastIndexOf('/')];
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
