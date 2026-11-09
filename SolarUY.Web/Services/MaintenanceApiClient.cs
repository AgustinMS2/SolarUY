using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.Web.Services;

// Spec 021: cliente HTTP para reservas de mantenimiento contra ApiService.
public class MaintenanceApiClient(HttpClient httpClient)
{
    public async Task<ServiceResult<Guid>> CreateReservationAsync(
        CreateMaintenanceReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/maintenances", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var doc = JsonDocument.Parse(content);
                var id = doc.RootElement.GetProperty("id").GetGuid();
                return ServiceResult<Guid>.Ok(id);
            }

            var (message, _) = await ReadErrorAsync(response, "Error al reservar el mantenimiento", cancellationToken);
            var errorType = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
                System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
                System.Net.HttpStatusCode.Conflict => ServiceErrorType.ConfirmationRequired,
                _ => ServiceErrorType.Validation
            };

            return ServiceResult<Guid>.Fail(errorType, message);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Unavailable, $"No se pudo comunicar con el servicio: {ex.Message}");
        }
    }

    private static async Task<(string Message, bool RequiresConfirmation)> ReadErrorAsync(
        HttpResponseMessage response,
        string fallback,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(content);
            var message = doc.RootElement.TryGetProperty("message", out var m) ? m.GetString() : null;
            var requiresConfirmation = doc.RootElement.TryGetProperty("requiresConfirmation", out var c)
                && c.ValueKind == JsonValueKind.True;
            return (message ?? $"{fallback} ({(int)response.StatusCode}).", requiresConfirmation);
        }
        catch (JsonException)
        {
            return ($"{fallback} ({(int)response.StatusCode}).", false);
        }
    }
}
