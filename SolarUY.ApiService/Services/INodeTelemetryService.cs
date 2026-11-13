using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio de dominio puro que calcula la telemetría eléctrica y térmica de un nodo solar
/// y evalúa los umbrales operativos de sobretemperatura y sincronismo de red (Spec 028).
/// </summary>
public interface INodeTelemetryService
{
    NodeTelemetryDto CalculateMetrics(
        SolarNode node,
        Inverter? inverter,
        double activePowerKw,
        double dcVoltage,
        double dcCurrent,
        double acVoltage,
        double frequencyHz,
        double temperatureC,
        DateTime timestampUtc);
}
