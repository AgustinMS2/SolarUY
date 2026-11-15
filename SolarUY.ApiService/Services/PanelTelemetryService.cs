using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Calcula la telemetría de un panel y evalúa las reglas de la Spec 029: potencia esperada
/// según la curva del calendario solar de la fecha (spec 036), eficiencia instantánea estimada y rendimiento bajo
/// (RF-3: potencia actual inferior al 40 % de la esperada a esa hora).
/// </summary>
public class PanelTelemetryService : IPanelTelemetryService
{
    private const double LowPerformanceRatio = 0.40;

    public double CalculateExpectedPowerW(int nominalPowerW, DateTime localTime)
    {
        // Misma curva y mismo amanecer y atardecer que usa el motor de simulación (spec 036).
        return nominalPowerW <= 0
            ? 0.0
            : Math.Round(nominalPowerW * SolarCalendar.Factor(localTime), 1);
    }

    public PanelTelemetryDto CalculateMetrics(
        SolarPanel panel,
        string? parkName,
        string? nodeName,
        double activePowerW,
        DateTime localTime,
        DateTime timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(panel);

        var activePower = Math.Max(0.0, activePowerW);
        var expectedPower = CalculateExpectedPowerW(panel.PowerW, localTime);

        var percentOfNominal = panel.PowerW > 0
            ? Math.Round(activePower / panel.PowerW * 100.0, 1)
            : 0.0;

        // Sin potencia esperada (noche) no hay referencia para estimar la eficiencia ni evaluar el rendimiento.
        double? estimatedEfficiency = expectedPower > 0.0
            ? Math.Round(panel.Efficiency * (activePower / expectedPower), 1)
            : null;

        return new PanelTelemetryDto
        {
            PanelId = panel.Id,
            SerieNumber = panel.SerieNumber,
            ParkName = parkName ?? string.Empty,
            NodeName = nodeName ?? string.Empty,
            NominalPowerW = panel.PowerW,
            NominalEfficiencyPercent = panel.Efficiency,
            ActivePowerW = activePower,
            ExpectedPowerW = expectedPower,
            PercentOfNominal = percentOfNominal,
            EstimatedEfficiencyPercent = estimatedEfficiency,
            IsLowPerformance = expectedPower > 0.0 && activePower < LowPerformanceRatio * expectedPower,
            TimestampUtc = timestampUtc
        };
    }
}
