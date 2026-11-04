namespace SolarUY.ApiService.Dtos;

public record CreateSolarPanelRequest(
    string? SerieNumber,
    int PowerW,
    double Efficiency,
    Guid SolarParkId
);
