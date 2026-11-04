using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeKeycloakUserRepository : IKeycloakUserRepository
{
    public List<KeycloakUser> Users { get; } = [];

    /// <summary>When true every call behaves as if Keycloak could not be reached.</summary>
    public bool IsUnavailable { get; set; }

    public int ReplaceCalls { get; private set; }

    public IReadOnlyList<KeycloakUser> GetAll()
    {
        ThrowIfUnavailable();
        return Users.ToList();
    }

    public KeycloakUser? GetById(string id)
    {
        ThrowIfUnavailable();
        return Users.FirstOrDefault(u => u.Id == id);
    }

    public void ReplaceSystemRole(string id, string role)
    {
        ThrowIfUnavailable();
        ReplaceCalls++;

        var index = Users.FindIndex(u => u.Id == id);
        if (index >= 0)
        {
            Users[index] = Users[index] with { Role = role };
        }
    }

    private void ThrowIfUnavailable()
    {
        if (IsUnavailable)
        {
            throw new AuthenticationServiceUnavailableException("Keycloak is down.");
        }
    }
}
