using System.Globalization;

namespace Reflect.Web.Models.ViewModels.ProviderDashboard.Shared;

public static class TextFormat
{
    /// <summary>"1 day", "3 days".</summary>
    public static string Count(int n, string singular, string plural = null) =>
        n.ToString(CultureInfo.InvariantCulture) + " " + (n == 1 ? singular : plural ?? singular + "s");
}
