using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que genera y emite periÃ³dicamente telemetrÃ­a en tiempo real
/// para parques solares a travÃ©s de SignalR (Spec 027, RF-2).
/// </summary>
public class ParkTelemetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ParkTelemetryHub> _hubContext;
    private readonly IParkTelemetryService _telemetryService;
    private readonly ILogger<ParkTelemetryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);

    public ParkTelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ParkTelemetryHub> hubContext,
        IParkTelemetryService telemetryService,
        ILogger<ParkTelemetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ParkTelemetryBackgroundService iniciado con intervalo de {Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EmitTelemetryCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error durante el ciclo de emisiÃ³n de telemetrÃ­a de parques");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ParkTelemetryBackgroundService detenido");
    }

    private async Task EmitTelemetryCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var parkRepo = scope.ServiceProvider.GetRequiredService<ISolarParkRepository>();
        var parks = parkRepo.GetAll();

        var localTime = GetUruguayLocalTime();
        var localHour = localTime.Hour + (localTime.Minute / 60.0);

        foreach (var park in parks.Where(p => p.Status == SolarParkStatus.Active))
        {
            double simulatedPower = 0.0;
            double simulatedEnergy = 0.0;

            if (localHour >= 6.0 && localHour < 20.0)
            {
                // Curva senoidal solar centrada a las 13:00 con variaciÃ³n suave (+- 3%)
                var solarAngle = ((localHour - 6.0) / 14.0) * Math.PI;
                var baseSolarFactor = Math.Sin(solarAngle);

                // VariaciÃ³n suave pseudo-aleatoria pero acotada
                var jitter = 0.97 + (Random.Shared.NextDouble() * 0.06);
                simulatedPower = Math.Round(park.MaxCapacityKw * baseSolarFactor * jitter, 1);
                simulatedPower = Math.Clamp(simulatedPower, 0.0, park.MaxCapacityKw);

                var progressRatio = (localHour - 6.0) / 14.0;
                simulatedEnergy = Math.Round(park.MaxCapacityKw * 5.0 * progressRatio, 1);
            }

            var telemetry = _telemetryService.CalculateMetrics(park, simulatedPower, simulatedEnergy, localTime);

            var groupName = ParkTelemetryHub.GetGroupName(park.Id);
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync(ParkTelemetryHub.ReceiveEventName, telemetry, cancellationToken);
        }
    }

    private static DateTime GetUruguayLocalTime()
    {
        var uruguayZones = new[] { "America/Montevideo", "Montevideo Standard Time" };
        foreach (var id in uruguayZones)
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) && tz != null)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
        }
        return DateTime.UtcNow.AddHours(-3);
    }
}

