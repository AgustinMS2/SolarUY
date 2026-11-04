using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SolarUY.Web.Security;

/// <summary>
/// Attaches the authenticated user's Bearer token to requests sent to SolarUY.ApiService (spec 015, RF-5).
/// </summary>
public class BearerTokenHandler(
    CircuitServicesAccessor circuitServicesAccessor,
    IHttpContextAccessor httpContextAccessor,
    KeycloakTokenService tokenService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync();
        var key = UserTokenStore.GetKey(user);

        if (key != null)
        {
            var accessToken = await tokenService.GetValidAccessTokenAsync(key, cancellationToken);
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<ClaimsPrincipal?> GetCurrentUserAsync()
    {
        // Inside an interactive circuit there is no HttpContext: the user comes from the circuit's AuthenticationStateProvider.
        var authStateProvider = circuitServicesAccessor.Services?.GetService<AuthenticationStateProvider>();
        if (authStateProvider != null)
        {
            var state = await authStateProvider.GetAuthenticationStateAsync();
            return state.User;
        }

        // During prerendering an HttpContext is available.
        return httpContextAccessor.HttpContext?.User;
    }
}
