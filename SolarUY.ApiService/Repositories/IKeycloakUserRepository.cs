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
}
