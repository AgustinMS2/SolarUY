using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

public record UpdateSolarPanelRequest(
    string? SerieNumber,
    [property: JsonRequired] int PowerW,
    [property: JsonRequired] double Efficiency,
    [property: JsonRequired] Guid SolarParkId
);
