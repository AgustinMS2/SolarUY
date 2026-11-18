using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Security;

namespace SolarUY.Tests.Fakes;

public class FakeKeycloakUserRepository : IKeycloakUserRepository
{
    public List<KeycloakUser> Users { get; } = [];
    public Dictionary<string, string> PasswordsByEmail { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>When true every call behaves as if Keycloak could not be reached.</summary>
    public bool IsUnavailable { get; set; }

    public int ReplaceCalls { get; private set; }
    public int CreateCalls { get; private set; }
    public int DisableCalls { get; private set; }

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

    public bool ExistsByEmail(string email)
    {
        ThrowIfUnavailable();
        var normalized = email.Trim().ToLowerInvariant();
        return Users.Any(u =>
            string.Equals(u.Email, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase));
    }

    public string CreateTechnicianUser(string email, string fullName, string password)
    {
        ThrowIfUnavailable();
        CreateCalls++;

        var normalized = email.Trim().ToLowerInvariant();
        var id = Guid.NewGuid().ToString();
        var user = new KeycloakUser(
            Id: id,
            Username: normalized,
            FullName: fullName.Trim(),
            Email: normalized,
            Enabled: true,
            Role: Roles.Technician);

        Users.Add(user);
        PasswordsByEmail[normalized] = password;
        return id;
    }

    public void DisableByEmail(string email)
    {
        ThrowIfUnavailable();
        DisableCalls++;

        var normalized = email.Trim().ToLowerInvariant();
        var index = Users.FindIndex(u =>
            string.Equals(u.Email, normalized, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(u.Username, normalized, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            Users[index] = Users[index] with { Enabled = false };
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
