namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Body de petición para corte/reanudación de inyección a la red (spec 033).
/// </summary>
public record CutInjectionRequest(
    string ConfirmationText,
    string? Reason
);