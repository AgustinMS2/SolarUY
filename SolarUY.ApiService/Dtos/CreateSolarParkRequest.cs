using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

public record CreateSolarParkRequest(
    string Name,
    [property: JsonRequired] double Latitude,
    [property: JsonRequired] double Longitude,
    [property: JsonRequired] double MaxCapacityKw,
    [property: JsonRequired] double PowerAlertThresholdKw
);
