namespace SolarUY.ApiService.Dtos;

public record SolarPanelDto(
    Guid Id,
    string SerieNumber,
    int PowerW,
    double Efficiency,
    string SolarParkName,
    string Status,
    Guid SolarParkId = default,
    string? SolarNodeName = null,
    Guid? SolarNodeId = null
);
