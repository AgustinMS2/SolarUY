namespace SolarUY.ApiService.Dtos;

public record CreateSolarParkRequest(
    string Name,
    double Latitude,
    double Longitude,
    double MaxCapacityKw,
    double PowerAlertThresholdKw
);
