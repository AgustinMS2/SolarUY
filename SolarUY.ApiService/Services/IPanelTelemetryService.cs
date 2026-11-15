using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio de dominio puro que calcula la telemetría de un panel solar: la potencia esperada
/// según la curva solar del día y la regla de rendimiento bajo (Spec 029).
/// </summary>
public interface IPanelTelemetryService
{
    /// <summary>
    /// Potencia que el panel debería entregar a la hora local dada (hora decimal, Uruguay):
    /// potencia de placa × sen(π × (hora − 6) / 14) entre las 6:00 y las 20:00; 0 W fuera de ese horario.
    /// </summary>
    double CalculateExpectedPowerW(int nominalPowerW, double localHour);

    PanelTelemetryDto CalculateMetrics(
        SolarPanel panel,
        string? parkName,
        string? nodeName,
        double activePowerW,
        double localHour,
        DateTime timestampUtc);
}
