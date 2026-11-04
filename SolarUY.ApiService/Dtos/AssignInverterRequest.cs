namespace SolarUY.ApiService.Dtos;

public record AssignInverterRequest(
    string? SerieNumber,
    string? BrandAndModel,
    double NominalCapacityKw,
    string? Endpoint,
    bool ConfirmReplace
);
