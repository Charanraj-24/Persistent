namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Clients;

public class ArtworkTileViewModel
{
    public Guid SessionId { get; set; }
    public string Svg { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public bool IsXr { get; set; }
    public string Date { get; set; } = string.Empty;
    public string Mood { get; set; }
    public int MoodLift { get; set; }
    public List<string> Words { get; set; } = new();
    public int Pieces { get; set; }
}
