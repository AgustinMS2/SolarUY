namespace SolarUY.ApiService.Dtos;

public record UpdateSolarPanelRequest(
    string? SerieNumber,
    int PowerW,
    double Efficiency,
    Guid SolarParkId
);
