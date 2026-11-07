// STAND-IN: fake sign-in for the isolated demo. Delete on merge (Entra External ID takes over).
namespace Reflect.Web.Standins.Security;

public sealed record DemoUser(Guid UserId, string EntraObjectId, string DisplayName, string Email, string Role, string Description);

public static class DemoUsers
{
    public const string ProviderRole = "Provider";
    public const string EndUserRole = "End_User";
    /// <summary>Matches the real Entra role claim for Facility Admin.</summary>
    public const string FacilityAdminRole = "Facility_Admin";

    public static readonly DemoUser ProviderEllison = new(
        Guid.Parse("8d3c2f0e-4a51-4f7b-9a43-1f2e8b6c0a01"), "demo-entra-ellison",
        "Dr. Maya Ellison, PsyD", "maya.ellison@demo.reflect.test", ProviderRole, "Provider, 18 clients");

    public static readonly DemoUser ProviderReyes = new(
        Guid.Parse("8d3c2f0e-4a51-4f7b-9a43-1f2e8b6c0a02"), "demo-entra-reyes",
        "Jordan Reyes, LCSW", "jordan.reyes@demo.reflect.test", ProviderRole, "Provider, 6 clients");

    public static readonly DemoUser FacilityAdmin = new(
        Guid.Parse("8d3c2f0e-4a51-4f7b-9a43-1f2e8b6c0a04"), "demo-entra-facadmin",
        "Pat Morgan", "pat.morgan@demo.reflect.test", FacilityAdminRole, "Facility Admin (billing totals, no client data)");

    public static readonly DemoUser EndUser = new(
        Guid.Parse("8d3c2f0e-4a51-4f7b-9a43-1f2e8b6c0a03"), "demo-entra-enduser",
        "Sam Taylor", "sam.taylor@demo.reflect.test", EndUserRole, "End User (no provider access)");

    public static readonly IReadOnlyList<DemoUser> All = new[] { ProviderEllison, ProviderReyes, FacilityAdmin, EndUser };

    public static DemoUser Find(string key) =>
        All.FirstOrDefault(u => string.Equals(u.EntraObjectId, key, StringComparison.Ordinal)) ?? ProviderEllison;
}

public static class DemoUserRouting
{
    /// <summary>Where each demo role lands after signing in.</summary>
    public static string HomeFor(DemoUser user) => user.Role switch
    {
        DemoUsers.ProviderRole => "/Provider/Home/Dashboard",
        DemoUsers.FacilityAdminRole => "/Provider/FacilityBilling/Index",
        _ => "/StandinAccount/AccessDenied"
    };
}
