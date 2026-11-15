using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que emite periódicamente la telemetría en tiempo real de los parques solares por
/// SignalR (Spec 027, RF-2). La potencia y la energía del día salen del motor de simulación (spec 036): la potencia es
/// la suma de los inversores de los nodos activos del parque.
/// </summary>
public class ParkTelemetryBackgroundService : SimulationEmitterBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<ParkTelemetryHub> _hubContext;
    private readonly IParkTelemetryService _telemetryService;
    private readonly ISolarSimulationEngine _engine;

    public ParkTelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<ParkTelemetryHub> hubContext,
        IParkTelemetryService telemetryService,
        ISolarSimulationEngine engine,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider,
        ILogger<ParkTelemetryBackgroundService> logger)
        : base(options, timeProvider, logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _telemetryService = telemetryService;
        _engine = engine;
    }

    protected override string ServiceName => nameof(ParkTelemetryBackgroundService);

    protected override async Task EmitCycleAsync(DateTime instantUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var parkRepo = scope.ServiceProvider.GetRequiredService<ISolarParkRepository>();
        var nodeRepo = scope.ServiceProvider.GetRequiredService<ISolarNodeRepository>();
        var inverterRepo = scope.ServiceProvider.GetRequiredService<IInverterRepository>();
        var alertPublisher = scope.ServiceProvider.GetRequiredService<IAlertPublisher>();
        var faults = FaultTimeline.From(scope.ServiceProvider.GetRequiredService<ISimulatedFaultRepository>().GetAll());

        var activeParks = parkRepo.GetAll().Where(p => p.Status == SolarParkStatus.Active).ToList();
        if (activeParks.Count == 0)
        {
            return;
        }

        // Inversores de los nodos activos, por parque.
        var activeNodes = nodeRepo.GetAll().Where(n => n.Status == SolarNodeStatus.Active).ToList();
        var inverters = inverterRepo.GetByNodeIds(activeNodes.Select(n => n.Id).ToList());
        var parkByNode = activeNodes.ToDictionary(n => n.Id, n => n.SolarParkId);
        var invertersByPark = inverters
            .Where(i => parkByNode.ContainsKey(i.SolarNodeId))
            .GroupBy(i => parkByNode[i.SolarNodeId])
            .ToDictionary(g => g.Key, g => g.Select(i => new SimulatedInverter(i.Id, i.NominalCapacityKw)).ToList());

        var localTime = SolarCalendar.ToLocal(instantUtc);

        foreach (var park in activeParks)
        {
            invertersByPark.TryGetValue(park.Id, out var parkInverters);
            parkInverters ??= [];

            var powerKw = Math.Round(_engine.ReadParkPowerKw(park.Id, parkInverters, instantUtc, faults), 1);
            var energyKwh = Math.Round(_engine.EnergyTodayKwh(park.Id, parkInverters, instantUtc, faults), 1);

            var telemetry = _telemetryService.CalculateMetrics(
                park, powerKw, energyKwh, localTime,
                hasEquipment: parkInverters.Count > 0,
                timestampUtc: instantUtc);

            await _hubContext.Clients
                .Group(ParkTelemetryHub.GetGroupName(park.Id))
                .SendAsync(ParkTelemetryHub.ReceiveEventName, telemetry, cancellationToken);

            // Spec 031: si la generación cae bajo el umbral con sol, se registra y emite la alerta.
            await alertPublisher.PublishAsync(AlertDetectionRules.FromParkTelemetry(telemetry), cancellationToken);
        }
    }
}
