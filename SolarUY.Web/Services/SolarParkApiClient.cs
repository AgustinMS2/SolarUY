using System.Net.Http.Json;

namespace SolarUY.Web.Services;

public record CreateSolarParkDto(
    string Name,
    double Latitude,
    double Longitude,
    double MaxCapacityKw,
    double PowerAlertThresholdKw
);

public class SolarParkApiResponse
{
    public bool Success { get; set; }
    public Guid? Id { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SolarParkApiClient(HttpClient httpClient)
{
    public async Task<SolarParkApiResponse> CreateAsync(CreateSolarParkDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/solar-parks", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<CreateSolarParkSuccessResponse>(cancellationToken: cancellationToken);
                return new SolarParkApiResponse { Success = true, Id = content?.Id };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var errorMessage = error?.Message ?? $"Error al registrar el parque ({response.StatusCode}).";
            return new SolarParkApiResponse { Success = false, ErrorMessage = errorMessage };
        }
        catch (Exception ex)
        {
            return new SolarParkApiResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    private sealed record CreateSolarParkSuccessResponse(Guid Id);
    private sealed record ErrorResponse(string? Message);
}
