namespace SolarUY.ApiService.Security;

/// <summary>
/// Realm roles defined in Keycloak (spec 017). The set is fixed: no dynamic roles.
/// </summary>
public static class Roles
{
    public const string Administrator = "Administrador";
    public const string Technician = "Técnico";

    /// <summary>Roles allowed to read (query) data.</summary>
    public const string Any = Administrator + "," + Technician;
}
