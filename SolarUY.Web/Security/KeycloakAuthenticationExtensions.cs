using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace SolarUY.Web.Security;

public static class KeycloakAuthenticationExtensions
{
    public const string CookieName = "SolarUY.Cookie";
    public const string SessionEndedItemKey = "SolarUY.SessionEnded";
    public const string SessionEndedUrl = "/?sesion=finalizada";

    public static WebApplicationBuilder AddKeycloakAuthentication(this WebApplicationBuilder builder)
    {
        // Aspire injects the Keycloak URL through WithReference.
        var keycloakUrl = builder.Configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
        var realm = builder.Configuration["Keycloak:Realm"];

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = async context =>
                {
                    // If the server lost the tokens (e.g. after a restart), the local session is no longer usable against the API.
                    var store = context.HttpContext.RequestServices.GetRequiredService<UserTokenStore>();
                    var key = UserTokenStore.GetKey(context.Principal);
                    if (key == null || !store.TryGet(key, out _))
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                        // Spec 016 RF-4: flag it so the pipeline can redirect to the public home with a notice.
                        context.HttpContext.Items[SessionEndedItemKey] = true;
                    }
                };
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = $"{keycloakUrl}/realms/{realm}";
                options.ClientId = builder.Configuration["Keycloak:ClientId"];
                options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;
                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.MapInboundClaims = false;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.Scope.Add("roles");

                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "roles";

                options.Events.OnTokenValidated = context =>
                {
                    var store = context.HttpContext.RequestServices.GetRequiredService<UserTokenStore>();
                    var timeProvider = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                    var key = UserTokenStore.GetKey(context.Principal);
                    var response = context.TokenEndpointResponse;

                    if (key != null && response != null && !string.IsNullOrEmpty(response.AccessToken))
                    {
                        var expiresIn = int.TryParse(response.ExpiresIn, out var seconds) ? seconds : 300;
                        store.Set(key, new UserTokens(
                            response.AccessToken,
                            response.RefreshToken,
                            timeProvider.GetUtcNow().AddSeconds(expiresIn)));
                    }

                    return Task.CompletedTask;
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<UserTokenStore>();
        builder.Services.AddSingleton<KeycloakTokenService>();
        builder.Services.AddHttpClient("keycloak");
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddSingleton<CircuitServicesAccessor>();
        builder.Services.AddScoped<CircuitHandler, ServicesAccessorCircuitHandler>();
        builder.Services.AddTransient<BearerTokenHandler>();

        return builder;
    }

    /// <summary>
    /// Spec 016 RF-4: when the session was ended elsewhere, page navigations are redirected to the public home with a notice.
    /// </summary>
    public static WebApplication UseSessionEndedRedirect(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            var isAccountFlow = path.StartsWithSegments("/account")
                || path.StartsWithSegments("/signin-oidc")
                || path.StartsWithSegments("/signout-callback-oidc");

            var isPageNavigation = HttpMethods.IsGet(context.Request.Method)
                && context.Request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);

            if (context.Items.ContainsKey(SessionEndedItemKey) && isPageNavigation && !isAccountFlow)
            {
                context.Response.Redirect(SessionEndedUrl);
                return;
            }

            await next();
        });

        return app;
    }

    public static WebApplication MapAccountEndpoints(this WebApplication app)
    {
        app.MapGet("/account/login", (string? returnUrl) =>
        {
            var redirectUri = IsLocalUrl(returnUrl) ? returnUrl! : "/";
            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = redirectUri },
                [OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        // Spec 016 RF-2: destroys the local cookie and ends the Keycloak session (Single Sign-Out).
        app.MapGet("/account/logout", (HttpContext httpContext, UserTokenStore tokenStore) =>
        {
            if (httpContext.User.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/");
            }

            var key = UserTokenStore.GetKey(httpContext.User);
            if (key != null)
            {
                tokenStore.Remove(key);
            }

            // The OIDC handler sends id_token_hint and uses RedirectUri as post_logout_redirect_uri.
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        // Spec 016 RF-4: used when the Keycloak session no longer exists, so only the local cookie is cleared.
        app.MapGet("/account/session-ended", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect(SessionEndedUrl);
        }).AllowAnonymous();

        return app;
    }

    private static bool IsLocalUrl(string? url)
    {
        return !string.IsNullOrEmpty(url)
            && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }
}
