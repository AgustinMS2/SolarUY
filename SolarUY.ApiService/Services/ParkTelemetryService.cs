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

        var effectivePower = (isDaylight && isActive)
            ? Math.Max(0.0, currentPowerKw)
            : 0.0;

        var percentage = (park.MaxCapacityKw > 0 && effectivePower > 0)
            ? Math.Round((effectivePower / park.MaxCapacityKw) * 100.0, 1)
            : 0.0;

        var isAlertActive = isActive && isDaylight && effectivePower < park.PowerAlertThresholdKw;

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
