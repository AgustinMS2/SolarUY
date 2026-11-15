using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

public class ParkTelemetryService : IParkTelemetryService
{
    public ParkTelemetryDto CalculateMetrics(
        SolarPark park,
        double currentPowerKw,
        double energyTodayKwh,
        DateTime localTimeUruguay,
        bool hasEquipment = true,
        DateTime? timestampUtc = null)
    {
        ArgumentNullException.ThrowIfNull(park);

        // Las horas de sol son las del calendario solar de la fecha (spec 036), no un horario fijo.
        var isDaylight = SolarCalendar.IsDaylight(localTimeUruguay);
        var isActive = park.Status == SolarParkStatus.Active;

        var effectivePower = (isDaylight && isActive)
            ? Math.Max(0.0, currentPowerKw)
            : 0.0;

        var percentage = (park.MaxCapacityKw > 0 && effectivePower > 0)
            ? Math.Round((effectivePower / park.MaxCapacityKw) * 100.0, 1)
            : 0.0;

        // Un parque sin equipos que generen no tiene de qué alertar (spec 036).
        var isAlertActive = isActive && hasEquipment && isDaylight && effectivePower < park.PowerAlertThresholdKw;

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
            TimestampUtc = timestampUtc ?? DateTime.UtcNow
        };
    }
}
