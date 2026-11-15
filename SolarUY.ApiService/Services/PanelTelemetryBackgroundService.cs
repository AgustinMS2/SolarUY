using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que emite periódicamente las lecturas de los paneles solares activos asignados a un nodo
/// activo, por SignalR (Spec 029, RF-2). La potencia de cada panel sale del motor de simulación (spec 036), con la
/// misma curva, el mismo calendario solar y la misma nubosidad del parque que el resto de las pantallas.
/// </summary>
public class PanelTelemetryBackgroundService : SimulationEmitterBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PanelTelemetryHub> _hubContext;
    private readonly IPanelTelemetryService _telemetryService;
    private readonly ISolarSimulationEngine _engine;

    public PanelTelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<PanelTelemetryHub> hubContext,
        IPanelTelemetryService telemetryService,
        ISolarSimulationEngine engine,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider,
        ILogger<PanelTelemetryBackgroundService> logger)
        : base(options, timeProvider, logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _telemetryService = telemetryService;
        _engine = engine;
    }

    protected override string ServiceName => nameof(PanelTelemetryBackgroundService);

    protected override async Task EmitCycleAsync(DateTime instantUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var panelRepo = scope.ServiceProvider.GetRequiredService<ISolarPanelRepository>();
        var faults = FaultTimeline.From(scope.ServiceProvider.GetRequiredService<ISimulatedFaultRepository>().GetAll());

        var localTime = SolarCalendar.ToLocal(instantUtc);

        // Solo paneles activos conectados a un nodo activo: el resto no emite lecturas (spec 029, RF-4 y casos límite).
        var panels = panelRepo.GetAll()
            .Where(p => p.Status == SolarPanelStatus.Active
                        && p.SolarNode is { Status: SolarNodeStatus.Active })
            .ToList();

        foreach (var panel in panels)
        {
            var activePowerW = Math.Round(_engine.ReadPanelPowerW(panel.Id, panel.SolarParkId, panel.PowerW, instantUtc, faults), 1);

            var telemetry = _telemetryService.CalculateMetrics(
                panel,
                panel.SolarPark?.Name,
                panel.SolarNode?.Name,
                activePowerW,
                localTime,
                instantUtc,
                faults.EffectiveFault(panel.Id, instantUtc));

            await _hubContext.Clients
                .Group(PanelTelemetryHub.GetGroupName(panel.Id))
                .SendAsync(PanelTelemetryHub.ReceiveEventName, telemetry, cancellationToken);
        }
    }
}
