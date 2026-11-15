using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Calcula la telemetría de un panel y evalúa las reglas de la Spec 029: potencia esperada
/// según la curva solar del día, eficiencia instantánea estimada y rendimiento bajo
/// (RF-3: potencia actual inferior al 40 % de la esperada a esa hora).
/// </summary>
public class PanelTelemetryService : IPanelTelemetryService
{
    // Misma curva solar que usan las simulaciones de las specs 027 y 028.
    private const double SunriseHour = 6.0;
    private const double SunsetHour = 20.0;
    private const double LowPerformanceRatio = 0.40;

    public double CalculateExpectedPowerW(int nominalPowerW, double localHour)
    {
        if (nominalPowerW <= 0 || localHour <= SunriseHour || localHour >= SunsetHour)
        {
            return 0.0;
        }

        var solarAngle = (localHour - SunriseHour) / (SunsetHour - SunriseHour) * Math.PI;
        return Math.Round(nominalPowerW * Math.Sin(solarAngle), 1);
    }

    public PanelTelemetryDto CalculateMetrics(
        SolarPanel panel,
        string? parkName,
        string? nodeName,
        double activePowerW,
        double localHour,
        DateTime timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(panel);

        var activePower = Math.Max(0.0, activePowerW);
        var expectedPower = CalculateExpectedPowerW(panel.PowerW, localHour);

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
