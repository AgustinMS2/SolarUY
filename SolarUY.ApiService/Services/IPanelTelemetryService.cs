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
    /// Potencia que el panel debería entregar en la fecha y hora locales dadas (Uruguay): potencia de placa × la curva
    /// del calendario solar (spec 036), con sol entre el amanecer y el atardecer de esa fecha; 0 W fuera de ese horario.
    /// </summary>
    double CalculateExpectedPowerW(int nominalPowerW, DateTime localTime);

    PanelTelemetryDto CalculateMetrics(
        SolarPanel panel,
        string? parkName,
        string? nodeName,
        double activePowerW,
        DateTime localTime,
        DateTime timestampUtc);
}
