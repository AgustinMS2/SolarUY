using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

public record CreateSolarNodeRequest(
    string Name,
    [property: JsonRequired] Guid SolarParkId
);