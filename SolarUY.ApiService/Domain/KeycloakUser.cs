namespace SolarUY.ApiService.Domain;

/// <summary>
/// A user as stored in the authentication service (Keycloak). <see cref="Role"/> is the user's system role
/// ("Administrador" or "Técnico"), or null when the user has none.
/// </summary>
public record KeycloakUser(
    string Id,
    string Username,
    string FullName,
    string Email,
    bool Enabled,
    string? Role
);
