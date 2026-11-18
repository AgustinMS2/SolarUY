using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

public record AssignInverterRequest(
    string? SerieNumber,
    string? BrandAndModel,
    [property: JsonRequired] double NominalCapacityKw,
    string? Endpoint,
    // Confirmación opcional: si el cliente no la envía vale false (no confirmó el reemplazo).
#pragma warning disable S6964 // Value type property used as input should be required
    bool ConfirmReplace
#pragma warning restore S6964
);
