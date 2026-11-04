using System.Collections.Concurrent;
using System.Security.Claims;

namespace SolarUY.Web.Security;

public sealed record UserTokens(string AccessToken, string? RefreshToken, DateTimeOffset AccessTokenExpiresAt);

/// <summary>
/// Keeps each Keycloak session's tokens in server memory. Blazor Server circuits
/// have no HttpContext, so they cannot read the tokens from the cookie.
/// </summary>
public class UserTokenStore
{
    private readonly ConcurrentDictionary<string, UserTokens> _tokens = new();

    public SemaphoreSlim RefreshLock { get; } = new(1, 1);

    public static string? GetKey(ClaimsPrincipal? user)
    {
        return user?.FindFirst("sid")?.Value ?? user?.FindFirst("sub")?.Value;
    }

    public bool TryGet(string key, out UserTokens tokens)
    {
        return _tokens.TryGetValue(key, out tokens!);
    }

    public void Set(string key, UserTokens tokens)
    {
        _tokens[key] = tokens;
    }

    public void Remove(string key)
    {
        _tokens.TryRemove(key, out _);
    }
}
