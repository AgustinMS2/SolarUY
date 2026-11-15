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

        // Spec 033: durante el corte administrativo el parque no entrega potencia a la red.
        var isInjecting = park.GridInjectionEnabled;

        var effectivePower = (isDaylight && isActive && isInjecting)
            ? Math.Max(0.0, currentPowerKw)
            : 0.0;

        var percentage = (park.MaxCapacityKw > 0 && effectivePower > 0)
            ? Math.Round((effectivePower / park.MaxCapacityKw) * 100.0, 1)
            : 0.0;

        // Sin alerta de generación baja si el parque no tiene equipos que generen (spec 036) ni durante el corte
        // administrativo de la inyección (spec 033, mismo tratamiento que un parque inactivo).
        var isAlertActive = isActive && hasEquipment && isDaylight && isInjecting && effectivePower < park.PowerAlertThresholdKw;

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
