using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que genera y emite periódicamente telemetría eléctrica
/// y térmica en tiempo real para los nodos solares activos con inversor asignado,
/// a través de SignalR (Spec 028, RF-2).
/// </summary>
public class NodeTelemetryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<NodeTelemetryHub> _hubContext;
    private readonly INodeTelemetryService _telemetryService;
    private readonly ILogger<NodeTelemetryBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);

    public NodeTelemetryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IHubContext<NodeTelemetryHub> hubContext,
        INodeTelemetryService telemetryService,
        ILogger<NodeTelemetryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _telemetryService = telemetryService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NodeTelemetryBackgroundService iniciado con intervalo de {Interval}s", _interval.TotalSeconds);

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
                _logger.LogWarning(ex, "Error durante el ciclo de emisión de telemetría de nodos");
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

        _logger.LogInformation("NodeTelemetryBackgroundService detenido");
    }

    private async Task EmitTelemetryCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var nodeRepo = scope.ServiceProvider.GetRequiredService<ISolarNodeRepository>();
        var inverterRepo = scope.ServiceProvider.GetRequiredService<IInverterRepository>();

        var activeNodes = nodeRepo.GetAll().Where(n => n.Status == SolarNodeStatus.Active).ToList();
        var invertersByNode = inverterRepo
            .GetByNodeIds(activeNodes.Select(n => n.Id).ToList())
            .ToDictionary(i => i.SolarNodeId);

        var localTime = GetUruguayLocalTime();
        var localHour = localTime.Hour + (localTime.Minute / 60.0);

        foreach (var node in activeNodes)
        {
            if (!invertersByNode.TryGetValue(node.Id, out var inverter))
            {
                // RF-5: los nodos sin inversor asignado no generan telemetría en vivo.
                continue;
            }

            var (activePowerKw, dcVoltage, dcCurrent, acVoltage, frequencyHz, temperatureC) =
                SimulateInverterReadings(inverter.NominalCapacityKw, localHour);

            var telemetry = _telemetryService.CalculateMetrics(
                node, inverter,
                activePowerKw, dcVoltage, dcCurrent,
                acVoltage, frequencyHz, temperatureC,
                timestampUtc: DateTime.UtcNow);

            var groupName = NodeTelemetryHub.GetGroupName(node.Id);
            await _hubContext.Clients
                .Group(groupName)
                .SendAsync(NodeTelemetryHub.ReceiveEventName, telemetry, cancellationToken);
        }
    }

    /// <summary>
    /// Simula lecturas realistas del inversor a partir de la curva solar del día:
    /// la potencia sigue una campana centrada al mediodía y la temperatura y la
    /// corriente acompañan a la carga; ocasionalmente se inyectan desvíos de
    /// frecuencia y sobretemperatura para ejercitar las alertas RF-3 y RF-4.
    /// </summary>
    private static (double ActivePowerKw, double DcVoltage, double DcCurrent, double AcVoltage, double FrequencyHz, double TemperatureC)
        SimulateInverterReadings(double nominalCapacityKw, double localHour)
    {
        var isDaylight = localHour >= 6.0 && localHour < 20.0;
        var solarAngle = isDaylight ? ((localHour - 6.0) / 14.0) * Math.PI : 0.0;
        var solarFactor = isDaylight ? Math.Sin(solarAngle) : 0.0;

        // Potencia activa: carga nominal atenuada por la curva solar y jitter suave (±3%).
        var jitterPower = 0.97 + (Random.Shared.NextDouble() * 0.06);
        var activePowerKw = Math.Round(nominalCapacityKw * solarFactor * jitterPower, 1);
        activePowerKw = Math.Clamp(activePowerKw, 0.0, nominalCapacityKw);

        // Tensión DC de entrada: caída leve al aumentar la carga (200–1000 V).
        var loadRatio = nominalCapacityKw > 0.0 ? activePowerKw / nominalCapacityKw : 0.0;
        var jitterVoltage = 0.98 + (Random.Shared.NextDouble() * 0.04);
        var dcVoltage = Math.Round((740.0 - (180.0 * loadRatio)) * jitterVoltage, 0);
        dcVoltage = Math.Clamp(dcVoltage, 200.0, 1000.0);

        // Corriente DC derivada de la potencia y la tensión de entrada (P = V * I).
        var dcCurrent = dcVoltage > 0.0 ? Math.Round((activePowerKw * 1000.0) / dcVoltage, 1) : 0.0;

        // Tensión AC de salida: red trifásica de 380 V con jitter leve.
        var jitterAc = 0.995 + (Random.Shared.NextDouble() * 0.01);
        var acVoltage = Math.Round(380.0 * jitterAc, 1);

        // Frecuencia de red: nominal 50 Hz; ~4% de las lecturas se desvía
        // fuera de 50 ± 0.5 Hz para ejercitar la alerta de sincronismo (RF-4).
        var frequencyHz = 50.0;
        if (Random.Shared.NextDouble() < 0.04)
        {
            frequencyHz = Random.Shared.NextDouble() < 0.5 ? 49.2 : 50.8;
        }
        else
        {
            var jitterFrequency = 49.9 + (Random.Shared.NextDouble() * 0.2);
            frequencyHz = Math.Round(jitterFrequency, 2);
        }

        // Temperatura interna: base de 30 °C más hasta 45 °C según carga,
        // con jitter; a plena carga puede superar los 75 °C de la RF-3.
        var jitterTemperature = 0.92 + (Random.Shared.NextDouble() * 0.16);
        var temperatureC = Math.Round(30.0 + (45.0 * loadRatio * jitterTemperature), 1);

        return (activePowerKw, dcVoltage, dcCurrent, acVoltage, frequencyHz, temperatureC);
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
