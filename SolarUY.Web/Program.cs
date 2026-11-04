using SolarUY.Web;
using SolarUY.Web.Components;
using SolarUY.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.AddKeycloakAuthentication();

builder.Services.AddHttpClient<WeatherApiClient>(client => client.BaseAddress = new("http://apiservice"));
builder.Services.AddHttpClient<SolarUY.Web.Services.SolarParkApiClient>(client => client.BaseAddress = new("http://apiservice"))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<SolarUY.Web.Services.PanelsApiClient>(client => client.BaseAddress = new("http://apiservice"))
    .AddHttpMessageHandler<BearerTokenHandler>();
builder.Services.AddHttpClient<SolarUY.ApiService.Services.SolarNodeApiClient>(client => client.BaseAddress = new("http://apiservice"))
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseAuthentication();
app.UseSessionEndedRedirect();
app.UseAuthorization();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapAccountEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
