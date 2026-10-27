namespace SolarUY.ApiService.Dtos;

public record SolarParkDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double MaxCapacityKw,
    double PowerAlertThresholdKw,
    string Status
);
