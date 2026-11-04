using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Dtos;

public record SolarNodeDto(
    Guid Id,
    string Name,
    Guid SolarParkId,
    string SolarParkName,
    int PanelsCount,
    string? InverterSerie,
    string Status
);