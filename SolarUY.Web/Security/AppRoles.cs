namespace SolarUY.Web.Security;

/// <summary>
/// Keycloak realm roles (spec 017). Kept in sync with the API; the Web does not reference the API project.
/// </summary>
public static class AppRoles
{
    public const string Administrator = "Administrador";
    public const string Technician = "Técnico";
    public const string Any = Administrator + "," + Technician;
}
