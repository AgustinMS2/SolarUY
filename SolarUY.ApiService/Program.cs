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

// JWT Bearer authentication against Keycloak (spec 015). Aspire injects the Keycloak URL through WithReference.
var keycloakUrl = builder.Configuration["services:keycloak:http:0"] ?? "http://localhost:8080";
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{keycloakUrl}/realms/SolarUY";
        options.Audience = "solaruy-api";
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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

