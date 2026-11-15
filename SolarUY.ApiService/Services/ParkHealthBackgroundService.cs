using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que emite periódicamente el estado de salud de los paneles de cada
/// parque activo a través de SignalR: un solo evento por parque y ciclo (Spec 030, RF-5).
/// </summary>
public class ParkHealthBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ParkHealthHub> _hubContext;
    private readonly ILogger<ParkHealthBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);

    public ParkHealthBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ParkHealthHub> hubContext,
        ILogger<ParkHealthBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ParkHealthBackgroundService iniciado con intervalo de {Interval}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EmitHealthCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error durante el ciclo de emisión del mapa de salud de parques");
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

        _logger.LogInformation("ParkHealthBackgroundService detenido");
    }

    private async Task EmitHealthCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IParkHealthSnapshotBuilder>();

        foreach (var update in builder.BuildUpdatesForActiveParks())
        {
            await _hubContext.Clients
                .Group(ParkHealthHub.GetGroupName(update.ParkId))
                .SendAsync(ParkHealthHub.ReceiveEventName, update, cancellationToken);

            await PublishCriticalPanelAlertsAsync(scope.ServiceProvider, update, cancellationToken);
        }
    }

    // Spec 031: cada panel en Falla Crítica (menos del 10 % de lo esperado con sol) genera una alerta Crítica.
    private static async Task PublishCriticalPanelAlertsAsync(IServiceProvider services, ParkHealthUpdateDto update, CancellationToken cancellationToken)
    {
        var critical = update.Panels.Where(r => r.State == PanelHealthState.Critical).ToList();
        if (critical.Count == 0)
        {
            return;
        }

        var park = services.GetRequiredService<ISolarParkRepository>().GetById(update.ParkId);
        var panels = services.GetRequiredService<ISolarPanelRepository>()
            .GetByIds(critical.Select(r => r.PanelId).ToList())
            .ToDictionary(p => p.Id);
        var alertPublisher = services.GetRequiredService<IAlertPublisher>();

        foreach (var reading in critical)
        {
            if (!panels.TryGetValue(reading.PanelId, out var panel))
            {
                continue;
            }

            await alertPublisher.PublishAsync(
                AlertDetectionRules.FromPanelHealth(reading, update.ParkId, park?.Name ?? string.Empty, panel.SerieNumber),
                cancellationToken);
        }
    }
}
