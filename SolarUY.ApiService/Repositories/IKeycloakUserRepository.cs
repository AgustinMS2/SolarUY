using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public interface IKeycloakUserRepository
{
    /// <exception cref="AuthenticationServiceUnavailableException">Keycloak could not be reached.</exception>
    IReadOnlyList<KeycloakUser> GetAll();

    /// <exception cref="AuthenticationServiceUnavailableException">Keycloak could not be reached.</exception>
    KeycloakUser? GetById(string id);

    /// <summary>
    /// Gives the user the given system role and removes the other one. Internal Keycloak roles are left untouched.
    /// </summary>
    /// <exception cref="AuthenticationServiceUnavailableException">Keycloak could not be reached.</exception>
    void ReplaceSystemRole(string id, string role);

    /// <summary>
    /// Checks whether a user with the given email or username already exists in the realm.
    /// </summary>
    /// <exception cref="AuthenticationServiceUnavailableException">Keycloak could not be reached.</exception>
    bool ExistsByEmail(string email);

    /// <summary>
    /// Creates an enabled user with the given email as username, sets a permanent password, and assigns the Técnico realm role.
    /// </summary>
    /// <exception cref="AuthenticationServiceUnavailableException">Keycloak could not be reached or rejected the request.</exception>
    string CreateTechnicianUser(string email, string fullName, string password);

    /// <summary>
    /// Disables the Keycloak user account matching the given email, if present.
    /// </summary>
    /// <exception cref="AuthenticationServiceUnavailableException">Keycloak could not be reached.</exception>
    void DisableByEmail(string email);
}
