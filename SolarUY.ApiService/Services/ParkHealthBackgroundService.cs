using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que emite periódicamente el estado de salud de los paneles de cada
/// parque activo a través de SignalR: un solo evento por parque y ciclo (Spec 030, RF-5). Las lecturas salen del
/// motor de simulación (spec 036), en los mismos instantes que el resto de las pantallas en vivo.
/// </summary>
public class ParkHealthBackgroundService : SimulationEmitterBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ParkHealthHub> _hubContext;

    public ParkHealthBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ParkHealthHub> hubContext,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider,
        ILogger<ParkHealthBackgroundService> logger)
        : base(options, timeProvider, logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
    }

    protected override string ServiceName => nameof(ParkHealthBackgroundService);

    protected override async Task EmitCycleAsync(DateTime instantUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IParkHealthSnapshotBuilder>();

        foreach (var update in builder.BuildUpdatesForActiveParks(instantUtc))
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
