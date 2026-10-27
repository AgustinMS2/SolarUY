namespace SolarUY.ApiService.Dtos;

public record UpdateSolarParkRequest(
    string Name,
    double Latitude,
    double Longitude,
    double MaxCapacityKw,
    double PowerAlertThresholdKw
);
