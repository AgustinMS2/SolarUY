using System.Net.Http.Json;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.Web.Services;

// Spec 031: cliente HTTP de alertas de falla contra ApiService.
public class AlertsApiClient(HttpClient httpClient)
{
    /// <summary>Alertas recientes y contador de Pendientes; nulo si el servicio no responde.</summary>
    public async Task<AlertListDto?> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<AlertListDto>($"/api/alerts?take={take}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public Task<ServiceResult<AlertDto>> AcknowledgeAsync(Guid id, CancellationToken cancellationToken = default)
        => PostAsync($"/api/alerts/{id}/acknowledge", "Error al reconocer la alerta", cancellationToken);

    public Task<ServiceResult<AlertDto>> DismissAsync(Guid id, CancellationToken cancellationToken = default)
        => PostAsync($"/api/alerts/{id}/dismiss", "Error al descartar la alerta", cancellationToken);

    private async Task<ServiceResult<AlertDto>> PostAsync(string url, string defaultError, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.PostAsync(url, content: null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var alert = await response.Content.ReadFromJsonAsync<AlertDto>(cancellationToken: cancellationToken);
                return alert != null
                    ? ServiceResult<AlertDto>.Ok(alert)
                    : ServiceResult<AlertDto>.Fail(ServiceErrorType.Unavailable, defaultError);
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            var message = error?.Message ?? defaultError;

            var errorType = response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
                System.Net.HttpStatusCode.Conflict => ServiceErrorType.Conflict,
                System.Net.HttpStatusCode.Forbidden => ServiceErrorType.Validation,
                _ => ServiceErrorType.Unavailable
            };

            return ServiceResult<AlertDto>.Fail(errorType, message);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<AlertDto>.Fail(ServiceErrorType.Unavailable, $"No se pudo comunicar con el servicio: {ex.Message}");
        }
    }

    private sealed record ErrorResponse(string? Message);
}
