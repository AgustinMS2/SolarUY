using System.Net.Http.Json;

namespace SolarUY.Web.Services;

public record CreateSolarParkDto(
    string Name,
    double Latitude,
    double Longitude,
    double MaxCapacityKw,
    double PowerAlertThresholdKw
);

public record SolarParkDto(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double MaxCapacityKw,
    double PowerAlertThresholdKw,
    string Status,
    bool GridInjectionEnabled = true
);

public record UpdateSolarParkDto(
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

sealed record CreateSolarParkSuccessResponse(Guid Id);
sealed record ErrorResponse(string? Message);

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

    public async Task<SolarParkApiResponse> UpdateAsync(Guid id, UpdateSolarParkDto request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/solar-parks/{id}", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new SolarParkApiResponse { Success = true, Id = id };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var errorMessage = error?.Message ?? $"Error al actualizar el parque ({response.StatusCode}).";
            return new SolarParkApiResponse { Success = false, ErrorMessage = errorMessage };
        }
        catch (Exception ex)
        {
            return new SolarParkApiResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    public async Task<SolarParkApiResponse> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/solar-parks/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new SolarParkApiResponse { Success = true, Id = id };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var errorMessage = error?.Message ?? $"Error al dar de baja el parque ({response.StatusCode}).";
            return new SolarParkApiResponse { Success = false, ErrorMessage = errorMessage };
        }
        catch (Exception ex)
        {
            return new SolarParkApiResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    public async Task<SolarParkDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<SolarParkDto>($"/api/solar-parks/{id}", cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <summary>Carga inicial del mapa de salud de paneles del parque (spec 030). Devuelve null si el parque no existe.</summary>
    public async Task<SolarUY.ApiService.Dtos.ParkHealthSnapshotDto?> GetPanelHealthAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<SolarUY.ApiService.Dtos.ParkHealthSnapshotDto>($"/api/solar-parks/{id}/panel-health", cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // Spec 033: corte inicial del parque
    public async Task<PanelApiResponse> CutInjectionAsync(Guid parkId, string confirmationText, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/solar-parks/{parkId}/cut-injection", new { ConfirmationText = confirmationText, Reason = reason }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new PanelApiResponse { Success = true };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var errorMessage = error?.Message ?? $"Error al procesar el corte de inyección ({response.StatusCode}).";
            return new PanelApiResponse { Success = false, ErrorMessage = errorMessage };
        }
        catch (Exception ex)
        {
            return new PanelApiResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    // Spec 033: reanudación del parque
    public async Task<PanelApiResponse> ResumeInjectionAsync(Guid parkId, string confirmationText, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"/api/solar-parks/{parkId}/resume-injection", new { ConfirmationText = confirmationText, Reason = reason }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new PanelApiResponse { Success = true };
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var errorMessage = error?.Message ?? $"Error al procesar la reanudación de inyección ({response.StatusCode}).";
            return new PanelApiResponse { Success = false, ErrorMessage = errorMessage };
        }
        catch (Exception ex)
        {
            return new PanelApiResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    public async Task<IReadOnlyList<SolarParkDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SolarParkDto>>("/api/solar-parks", cancellationToken);
        return result ?? [];
    }
}