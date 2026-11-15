using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que genera y emite periódicamente lecturas simuladas de los
/// paneles solares activos asignados a un nodo activo, a través de SignalR (Spec 029, RF-2).
/// </summary>
public class PanelTelemetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PanelTelemetryHub> _hubContext;
    private readonly IPanelTelemetryService _telemetryService;
    private readonly ILogger<PanelTelemetryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);

    public PanelTelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<PanelTelemetryHub> hubContext,
        IPanelTelemetryService telemetryService,
        ILogger<PanelTelemetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PanelTelemetryBackgroundService iniciado con intervalo de {Interval}s", _interval.TotalSeconds);

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
                _logger.LogWarning(ex, "Error durante el ciclo de emisión de telemetría de paneles");
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

        _logger.LogInformation("PanelTelemetryBackgroundService detenido");
    }

    private async Task EmitTelemetryCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var panelRepo = scope.ServiceProvider.GetRequiredService<ISolarPanelRepository>();

        var localTime = GetUruguayLocalTime();
        var localHour = localTime.Hour + (localTime.Minute / 60.0);

        // Solo paneles activos conectados a un nodo activo: el resto no emite lecturas (spec 029, RF-4 y casos límite).
        var panels = panelRepo.GetAll()
            .Where(p => p.Status == SolarPanelStatus.Active
                        && p.SolarNode is { Status: SolarNodeStatus.Active })
            .ToList();

        foreach (var panel in panels)
        {
            var expectedPowerW = _telemetryService.CalculateExpectedPowerW(panel.PowerW, localHour);
            var activePowerW = SimulateActivePowerW(expectedPowerW);

            var telemetry = _telemetryService.CalculateMetrics(
                panel,
                panel.SolarPark?.Name,
                panel.SolarNode?.Name,
                activePowerW,
                localHour,
                DateTime.UtcNow);

            await _hubContext.Clients
                .Group(PanelTelemetryHub.GetGroupName(panel.Id))
                .SendAsync(PanelTelemetryHub.ReceiveEventName, telemetry, cancellationToken);
        }
    }

    /// <summary>
    /// Simula la lectura de un panel a partir de lo esperado a esa hora: variación leve (±3 %)
    /// y, en ~5 % de las lecturas, una caída fuerte (sombreado o suciedad) para ejercitar
    /// la alerta de rendimiento bajo (RF-3).
    /// </summary>
    private static double SimulateActivePowerW(double expectedPowerW)
    {
        if (expectedPowerW <= 0.0)
        {
            return 0.0;
        }

        var factor = Random.Shared.NextDouble() < 0.05
            ? 0.20 + (Random.Shared.NextDouble() * 0.15)
            : 0.97 + (Random.Shared.NextDouble() * 0.06);

        return Math.Round(expectedPowerW * factor, 1);
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
