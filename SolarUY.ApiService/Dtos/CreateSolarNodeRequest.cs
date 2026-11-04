namespace SolarUY.ApiService.Dtos;

public record CreateSolarNodeRequest(
    string Name,
    Guid SolarParkId
);