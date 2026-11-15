using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public class ParkTelemetryService : IParkTelemetryService
{
    public ParkTelemetryDto CalculateMetrics(
        SolarPark park,
        double currentPowerKw,
        double energyTodayKwh,
        DateTime localTimeUruguay)
    {
        ArgumentNullException.ThrowIfNull(park);

        var isDaylight = IsDaylightHour(localTimeUruguay);
        var isActive = park.Status == SolarParkStatus.Active;

        // Spec 033: durante el corte administrativo el parque no entrega potencia a la red.
        var isInjecting = park.GridInjectionEnabled;

        var effectivePower = (isDaylight && isActive && isInjecting)
            ? Math.Max(0.0, currentPowerKw)
            : 0.0;

        var percentage = (park.MaxCapacityKw > 0 && effectivePower > 0)
            ? Math.Round((effectivePower / park.MaxCapacityKw) * 100.0, 1)
            : 0.0;

        // El corte administrativo no genera alertas de generación baja (mismo tratamiento que un parque inactivo).
        var isAlertActive = isActive && isDaylight && isInjecting && effectivePower < park.PowerAlertThresholdKw;

        return new ParkTelemetryDto
        {
            ParkId = park.Id,
            ParkName = park.Name,
            CurrentPowerKw = effectivePower,
            MaxCapacityKw = park.MaxCapacityKw,
            CapacityPercentage = percentage,
            EnergyTodayKwh = Math.Max(0.0, energyTodayKwh),
            AlertThresholdKw = park.PowerAlertThresholdKw,
            IsAlertActive = isAlertActive,
            TimestampUtc = DateTime.UtcNow
        };
    }

    private static bool IsDaylightHour(DateTime localTime)
    {
        // En Uruguay el rango diurno operativo solar se define entre las 06:00 y las 20:00
        return localTime.Hour >= 6 && localTime.Hour < 20;
    }
}
