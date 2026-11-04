using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.AddNpgsqlDbContext<SolarUY.ApiService.Data.SolarUyDbContext>("solaruydb");
builder.Services.AddScoped<SolarUY.ApiService.Repositories.ISolarParkRepository, SolarUY.ApiService.Repositories.SolarParkRepository>();
builder.Services.AddScoped<SolarUY.ApiService.Repositories.ISolarPanelRepository, SolarUY.ApiService.Repositories.SolarPanelRepository>();
builder.Services.AddScoped<SolarUY.ApiService.Services.ISolarParkService, SolarUY.ApiService.Services.SolarParkService>();
builder.Services.AddScoped<SolarUY.ApiService.Services.ISolarPanelService, SolarUY.ApiService.Services.SolarPanelService>();
builder.Services.AddScoped<SolarUY.ApiService.Repositories.ISolarNodeRepository, SolarUY.ApiService.Repositories.SolarNodeRepository>();
builder.Services.AddScoped<SolarUY.ApiService.Repositories.IInverterRepository, SolarUY.ApiService.Repositories.InverterRepository>();
builder.Services.AddScoped<SolarUY.ApiService.Services.ISolarNodeService, SolarUY.ApiService.Services.SolarNodeService>();

// Access to the Keycloak Admin API for user role management (spec 017).
builder.Services.TryAddSingleton(TimeProvider.System);
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHttpClient("keycloak-admin")
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });
}
else
{
    builder.Services.AddHttpClient("keycloak-admin");
}
builder.Services.AddSingleton<SolarUY.ApiService.Repositories.IKeycloakUserRepository, SolarUY.ApiService.Repositories.KeycloakUserRepository>();
builder.Services.AddScoped<SolarUY.ApiService.Services.IUserRoleService, SolarUY.ApiService.Services.UserRoleService>();

// JWT Bearer authentication against Keycloak (spec 015). Aspire injects the Keycloak URL through WithReference.
var keycloakUrl = builder.Configuration["services:keycloak:https:0"] ?? builder.Configuration["services:keycloak:http:0"] ?? "https://localhost:8080";
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakUrl}/realms/SolarUY";
        options.Audience = "solaruy-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        if (builder.Environment.IsDevelopment())
        {
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        // Spec 017: Keycloak realm roles arrive in the flat "roles" claim (realm mapper "realm-roles-claim").
        // Inbound claim mapping is disabled so the claim keeps the name "roles" instead of being renamed.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.RoleClaimType = "roles";

        // Spec 017 RF-4: a valid token without the required role gets 403 with a JSON message.
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnForbidden = context =>
            {
                context.Response.ContentType = "application/json";
                return context.Response.WriteAsJsonAsync(new { message = "Permisos insuficientes." });
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public partial class Program { }

