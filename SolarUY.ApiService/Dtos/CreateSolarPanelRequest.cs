using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

public record CreateSolarPanelRequest(
    string? SerieNumber,
    [property: JsonRequired] int PowerW,
    [property: JsonRequired] double Efficiency,
    [property: JsonRequired] Guid SolarParkId
);
