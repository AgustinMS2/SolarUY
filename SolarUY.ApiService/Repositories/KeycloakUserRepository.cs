using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Reads and updates users through the Keycloak Admin REST API using a dedicated service-account client
/// (client credentials flow). Calls are synchronous to follow the project's synchronous programming model.
/// </summary>
public class KeycloakUserRepository : IKeycloakUserRepository
{
    private const int MaxUsers = 1000;
    private static readonly TimeSpan TokenMargin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10);
    // Keycloak's JSON uses camelCase property names (id, name, firstName...).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<KeycloakUserRepository> _logger;
    private readonly object _tokenLock = new();

    private string? _accessToken;
    private DateTimeOffset _accessTokenExpiresAt;

    public KeycloakUserRepository(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<KeycloakUserRepository> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private string BaseUrl => _configuration["services:keycloak:https:0"] ?? _configuration["services:keycloak:http:0"] ?? "https://localhost:8080";

    private string Realm => _configuration["Keycloak:Realm"] ?? "SolarUY";

    public IReadOnlyList<KeycloakUser> GetAll()
    {
        var users = Read<List<UserRepresentation>>(Send(HttpMethod.Get, $"users?max={MaxUsers}")) ?? [];
        var administratorIds = GetRoleMemberIds(Roles.Administrator);
        var technicianIds = GetRoleMemberIds(Roles.Technician);

        return users
            .Select(u => ToUser(u, administratorIds.Contains(u.Id)
                ? Roles.Administrator
                : technicianIds.Contains(u.Id) ? Roles.Technician : null))
            .ToList();
    }

    public KeycloakUser? GetById(string id)
    {
        using var response = Send(HttpMethod.Get, $"users/{Uri.EscapeDataString(id)}", allowNotFound: true);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        var user = Read<UserRepresentation>(response);
        if (user == null)
        {
            return null;
        }

        var mappings = Read<List<RoleRepresentation>>(
            Send(HttpMethod.Get, $"users/{Uri.EscapeDataString(id)}/role-mappings/realm")) ?? [];

        // A user with both system roles keeps the union of permissions, so Administrador wins for display.
        string? role = null;
        if (mappings.Any(r => r.Name == Roles.Administrator))
        {
            role = Roles.Administrator;
        }
        else if (mappings.Any(r => r.Name == Roles.Technician))
        {
            role = Roles.Technician;
        }

        return ToUser(user, role);
    }

    public void ReplaceSystemRole(string id, string role)
    {
        var otherRole = role == Roles.Administrator ? Roles.Technician : Roles.Administrator;
        var newRepresentation = GetRole(role);
        var otherRepresentation = GetRole(otherRole);
        var path = $"users/{Uri.EscapeDataString(id)}/role-mappings/realm";

        // Add first, then remove: a failure in the middle never leaves the user without a role.
        Send(HttpMethod.Post, path, new[] { newRepresentation }).Dispose();
        Send(HttpMethod.Delete, path, new[] { otherRepresentation }).Dispose();

        _logger.LogInformation("System role of user {UserId} replaced by {Role} in Keycloak", id, role);
    }

    private RoleRepresentation GetRole(string name)
    {
        return Read<RoleRepresentation>(Send(HttpMethod.Get, $"roles/{Uri.EscapeDataString(name)}"))
            ?? throw new AuthenticationServiceUnavailableException($"Keycloak returned no data for role '{name}'.");
    }

    private HashSet<string> GetRoleMemberIds(string role)
    {
        var members = Read<List<UserRepresentation>>(
            Send(HttpMethod.Get, $"roles/{Uri.EscapeDataString(role)}/users?max={MaxUsers}")) ?? [];
        return members.Select(u => u.Id).ToHashSet();
    }

    private static KeycloakUser ToUser(UserRepresentation user, string? role)
    {
        var username = user.Username ?? string.Empty;
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return new KeycloakUser(
            user.Id,
            username,
            string.IsNullOrEmpty(fullName) ? username : fullName,
            user.Email ?? string.Empty,
            user.Enabled,
            role);
    }

    private HttpResponseMessage Send(HttpMethod method, string path, object? body = null, bool allowNotFound = false)
    {
        using var request = new HttpRequestMessage(method, $"{BaseUrl}/admin/realms/{Realm}/{path}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GetAdminToken());
        if (body != null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        var response = Execute(request);
        if (response.IsSuccessStatusCode || (allowNotFound && response.StatusCode == HttpStatusCode.NotFound))
        {
            return response;
        }

        _logger.LogWarning("Keycloak Admin API answered {StatusCode} for {Method} {Path}", (int)response.StatusCode, method, path);
        response.Dispose();
        throw new AuthenticationServiceUnavailableException(
            $"Keycloak Admin API answered {(int)response.StatusCode} for {method} {path}.");
    }

    private HttpResponseMessage Execute(HttpRequestMessage request)
    {
        try
        {
            using var timeout = new CancellationTokenSource(RequestTimeout);
            return _httpClientFactory.CreateClient("keycloak-admin").Send(request, timeout.Token);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not reach Keycloak at {Url}", request.RequestUri);
            throw new AuthenticationServiceUnavailableException("Could not reach Keycloak.", ex);
        }
    }

    private static T? Read<T>(HttpResponseMessage response)
    {
        using (response)
        {
            using var stream = response.Content.ReadAsStream();
            return JsonSerializer.Deserialize<T>(stream, JsonOptions);
        }
    }

    private string GetAdminToken()
    {
        lock (_tokenLock)
        {
            if (_accessToken != null && _accessTokenExpiresAt - _timeProvider.GetUtcNow() > TokenMargin)
            {
                return _accessToken;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{BaseUrl}/realms/{Realm}/protocol/openid-connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _configuration["Keycloak:AdminClientId"] ?? string.Empty,
                    ["client_secret"] = _configuration["Keycloak:AdminClientSecret"] ?? string.Empty
                })
            };

            using var response = Execute(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Keycloak rejected the admin client credentials ({StatusCode})", (int)response.StatusCode);
                throw new AuthenticationServiceUnavailableException(
                    $"Keycloak rejected the admin client credentials ({(int)response.StatusCode}).");
            }

            using var stream = response.Content.ReadAsStream();
            var token = JsonSerializer.Deserialize<TokenResponse>(stream, JsonOptions);
            if (token == null || string.IsNullOrEmpty(token.AccessToken))
            {
                throw new AuthenticationServiceUnavailableException("Keycloak returned no access token.");
            }

            _accessToken = token.AccessToken;
            _accessTokenExpiresAt = _timeProvider.GetUtcNow().AddSeconds(token.ExpiresIn);
            return _accessToken;
        }
    }

    internal sealed record UserRepresentation(
        string Id,
        string? Username,
        string? FirstName,
        string? LastName,
        string? Email,
        bool Enabled);

    internal sealed record RoleRepresentation(string Id, string Name);

    internal sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
