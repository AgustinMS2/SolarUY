using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que emite periódicamente la telemetría eléctrica y térmica en tiempo real de los nodos
/// solares activos con inversor asignado, por SignalR (Spec 028, RF-2). Las lecturas salen del motor de simulación
/// (spec 036), el mismo que usan el parque, el panel y el mapa de salud.
/// </summary>
public class NodeTelemetryBackgroundService : SimulationEmitterBackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NodeTelemetryHub> _hubContext;
    private readonly INodeTelemetryService _telemetryService;
    private readonly ISolarSimulationEngine _engine;

    public NodeTelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<NodeTelemetryHub> hubContext,
        INodeTelemetryService telemetryService,
        ISolarSimulationEngine engine,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider,
        ILogger<NodeTelemetryBackgroundService> logger)
        : base(options, timeProvider, logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _telemetryService = telemetryService;
        _engine = engine;
    }

    protected override string ServiceName => nameof(NodeTelemetryBackgroundService);

    protected override async Task EmitCycleAsync(DateTime instantUtc, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var nodeRepo = scope.ServiceProvider.GetRequiredService<ISolarNodeRepository>();
        var inverterRepo = scope.ServiceProvider.GetRequiredService<IInverterRepository>();
        var alertPublisher = scope.ServiceProvider.GetRequiredService<IAlertPublisher>();

        var activeNodes = nodeRepo.GetAll().Where(n => n.Status == SolarNodeStatus.Active).ToList();
        var invertersByNode = inverterRepo
            .GetByNodeIds(activeNodes.Select(n => n.Id).ToList())
            .ToDictionary(i => i.SolarNodeId);

        foreach (var node in activeNodes)
        {
            if (!invertersByNode.TryGetValue(node.Id, out var inverter))
            {
                // RF-5: los nodos sin inversor asignado no generan telemetría en vivo.
                continue;
            }

            var reading = _engine.ReadInverter(inverter.Id, node.SolarParkId, inverter.NominalCapacityKw, instantUtc);

            var telemetry = _telemetryService.CalculateMetrics(
                node, inverter,
                Math.Round(reading.PowerKw, 1),
                Math.Round(reading.DcVoltageV, 0),
                Math.Round(reading.DcCurrentA, 1),
                Math.Round(reading.AcVoltageV, 1),
                Math.Round(reading.FrequencyHz, 2),
                Math.Round(reading.TemperatureC, 1),
                timestampUtc: instantUtc);

            await _hubContext.Clients
                .Group(NodeTelemetryHub.GetGroupName(node.Id))
                .SendAsync(NodeTelemetryHub.ReceiveEventName, telemetry, cancellationToken);

            // Spec 031: si el inversor supera los 75 °C, se registra y emite la alerta.
            await alertPublisher.PublishAsync(
                AlertDetectionRules.FromNodeTelemetry(telemetry, node.SolarParkId, node.SolarPark?.Name ?? string.Empty),
                cancellationToken);
        }
    }
}
