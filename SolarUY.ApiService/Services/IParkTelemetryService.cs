using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface IParkTelemetryService
{
    ParkTelemetryDto CalculateMetrics(
        SolarPark park,
        double currentPowerKw,
        double energyTodayKwh,
        DateTime localTimeUruguay);
}

