using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace SolarUY.Web.Security;

/// <summary>
/// Returns a session's current access token, renewing it with the refresh token when it is about to expire.
/// </summary>
public class KeycloakTokenService(
    UserTokenStore store,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<KeycloakTokenService> logger)
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromSeconds(30);

    public async Task<string?> GetValidAccessTokenAsync(string key, CancellationToken cancellationToken)
    {
        if (!store.TryGet(key, out var tokens))
        {
            return null;
        }

        if (!IsExpiring(tokens))
        {
            return tokens.AccessToken;
        }

        await store.RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Another thread may have renewed the token while we waited for the lock.
            if (!store.TryGet(key, out tokens))
            {
                return null;
            }

            if (!IsExpiring(tokens))
            {
                return tokens.AccessToken;
            }

            var refreshed = await RefreshAsync(tokens, cancellationToken);
            if (refreshed == null)
            {
                store.Remove(key);
                return null;
            }

            store.Set(key, refreshed);
            return refreshed.AccessToken;
        }
        finally
        {
            store.RefreshLock.Release();
        }
    }

    private bool IsExpiring(UserTokens tokens)
    {
        return tokens.AccessTokenExpiresAt - timeProvider.GetUtcNow() <= RefreshMargin;
    }

    private async Task<UserTokens?> RefreshAsync(UserTokens current, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(current.RefreshToken))
        {
            return null;
        }

        var keycloakUrl = configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
        var realm = configuration["Keycloak:Realm"];
        var tokenEndpoint = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";

        using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = configuration["Keycloak:ClientId"]!,
                ["client_secret"] = configuration["Keycloak:ClientSecret"]!,
                ["refresh_token"] = current.RefreshToken
            })
        };

        try
        {
            var client = httpClientFactory.CreateClient("keycloak");
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Keycloak rejected the token refresh ({StatusCode}).", response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
            if (body == null || string.IsNullOrEmpty(body.AccessToken))
            {
                return null;
            }

            return new UserTokens(
                body.AccessToken,
                body.RefreshToken ?? current.RefreshToken,
                timeProvider.GetUtcNow().AddSeconds(body.ExpiresIn));
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Could not reach Keycloak to refresh the token.");
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
