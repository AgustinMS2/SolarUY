using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Calcula la telemetría del nodo e inversor y evalúa los umbrales de la Spec 028:
/// sobretemperatura interna (&gt; 75 °C, RF-3), anomalía de frecuencia de red
/// (fuera de 50 ± 0.5 Hz, RF-4) y supresión de lecturas y alarmas para nodos
/// inactivos o sin inversor asignado (RF-5).
/// </summary>
public class NodeTelemetryService : INodeTelemetryService
{
    private const double OverheatingThresholdC = 75.0;
    private const double MinGridFrequencyHz = 49.5;
    private const double MaxGridFrequencyHz = 50.5;

    public NodeTelemetryDto CalculateMetrics(
        SolarNode node,
        Inverter? inverter,
        double activePowerKw,
        double dcVoltage,
        double dcCurrent,
        double acVoltage,
        double frequencyHz,
        double temperatureC,
        DateTime timestampUtc,
        SimulatedFaultType? fault = null)
    {
        ArgumentNullException.ThrowIfNull(node);

        // Nodo inactivo, sin inversor o con la inyección a la red detenida administrativamente
        // (spec 033): el inversor no entrega potencia, por lo que se reportan los valores
        // eléctricos en cero y se suprimen alarmas espurias.
        // Con la falla de aislamiento (spec 037) el inversor se apagó por la protección de fuga a tierra: igual que detenido.
        var isOperating = node.Status == SolarNodeStatus.Active
            && inverter is not null
            && inverter.GridInjectionEnabled
            && fault != SimulatedFaultType.InsulationFault;

        return new NodeTelemetryDto
        {
            NodeId = node.Id,
            NodeName = node.Name,
            InverterSerieNumber = inverter?.SerieNumber ?? string.Empty,
            NominalCapacityKw = inverter?.NominalCapacityKw ?? 0.0,
            ActivePowerKw = isOperating ? activePowerKw : 0.0,
            DcVoltage = isOperating ? dcVoltage : 0.0,
            DcCurrent = isOperating ? dcCurrent : 0.0,
            AcVoltage = isOperating ? acVoltage : 0.0,
            GridFrequencyHz = isOperating ? frequencyHz : 0.0,
            InverterTemperatureC = isOperating ? temperatureC : 0.0,
            IsOverheating = isOperating && temperatureC > OverheatingThresholdC,
            IsGridFrequencyAnomaly = isOperating &&
                (frequencyHz < MinGridFrequencyHz || frequencyHz > MaxGridFrequencyHz),
            Fault = fault,
            FaultLabel = fault.HasValue ? FaultCatalog.LabelOf(fault.Value) : null,
            TimestampUtc = timestampUtc
        };
    }
}
