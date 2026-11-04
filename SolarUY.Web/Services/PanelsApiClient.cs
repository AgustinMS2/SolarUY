using System.Net.Http.Json;

namespace SolarUY.Web.Services;

public record CreateSolarPanelDto(
    string SerieNumber,
    int PowerW,
    double Efficiency,
    Guid SolarParkId
);

public class PanelApiResponse
{
    public bool Success { get; set; }
    public Guid? Id { get; set; }
    public string? ErrorMessage { get; set; }
}

public record SolarPanelDto(
    Guid Id,
    string SerieNumber,
    int PowerW,
    double Efficiency,
    string SolarParkName,
    string Status
);

public class PanelsApiClient(HttpClient httpClient)
{
    public async Task<PanelApiResponse> CreateAsync(CreateSolarPanelDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/panels", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<CreatePanelSuccessResponse>(cancellationToken: cancellationToken);
                return new PanelApiResponse { Success = true, Id = content?.Id };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var errorMessage = error?.Message ?? $"Error al registrar el panel ({response.StatusCode}).";
            return new PanelApiResponse { Success = false, ErrorMessage = errorMessage };
        }
        catch (Exception ex)
        {
            return new PanelApiResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    public async Task<IReadOnlyList<SolarPanelDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SolarPanelDto>>("/api/panels", cancellationToken);
        return result ?? [];
    }

    private sealed record CreatePanelSuccessResponse(Guid Id);
    private sealed record ErrorResponse(string? Message);
}
