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

    // Spec 024 (RF-1 a RF-4): listado con filtros combinables.
    public async Task<ServiceResult<IReadOnlyList<MaintenanceOrderDto>>> GetAllAsync(
        MaintenanceFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "/api/maintenances";
            var queryString = CollectFilterQuery(filter);
            if (queryString.Length > 0)
            {
                url += $"?{queryString}";
            }

            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var orders = await response.Content.ReadFromJsonAsync<List<MaintenanceOrderDto>>(cancellationToken: cancellationToken);
                return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Ok(orders ?? []);
            }

            var (message, _) = await ReadErrorAsync(response, "Error al obtener la lista de mantenimientos", cancellationToken);
            return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Fail(ServiceErrorType.Validation, message);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Fail(ServiceErrorType.Unavailable, $"No se pudo comunicar con el servicio: {ex.Message}");
        }
    }

    private static string CollectFilterQuery(MaintenanceFilter? filter)
    {
        if (filter == null)
        {
            return "";
        }

        var parts = new List<string>();

        if (filter.Status.HasValue)
        {
            parts.Add($"status={(int)filter.Status.Value}");
        }

        if (filter.ParkId.HasValue)
        {
            parts.Add($"parkId={filter.ParkId.Value}");
        }

        if (filter.TechnicianId.HasValue)
        {
            // El endpoint de consulta agrupa por query "techId" (spec 024 plan).
            parts.Add($"techId={filter.TechnicianId.Value}");
        }

        if (filter.FromUtc.HasValue)
        {
            parts.Add($"from={Uri.EscapeDataString(filter.FromUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
        }

        if (filter.ToUtc.HasValue)
        {
            parts.Add($"to={Uri.EscapeDataString(filter.ToUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
        }

        return string.Join("&", parts);
    }

    // Spec 022 (RF-1): precarga de la reserva a editar.
    public async Task<ServiceResult<MaintenanceOrderDto>> GetOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync($"/api/maintenances/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var order = await response.Content.ReadFromJsonAsync<MaintenanceOrderDto>(cancellationToken: cancellationToken);
                return ServiceResult<MaintenanceOrderDto>.Ok(order!);
            }

            var (message, _) = await ReadErrorAsync(response, "Error al obtener el mantenimiento", cancellationToken);
            var errorType = response.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
                _ => ServiceErrorType.Validation
            };

            return ServiceResult<MaintenanceOrderDto>.Fail(errorType, message);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult<MaintenanceOrderDto>.Fail(ServiceErrorType.Unavailable, $"No se pudo comunicar con el servicio: {ex.Message}");
        }
    }

    // Spec 022 (RF-2 a RF-4): modificación de una reserva pendiente.
    public async Task<ServiceResult> UpdateReservationAsync(
        Guid id,
        UpdateMaintenanceReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/maintenances/{id}", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ServiceResult.Ok();
            }

            var (message, _) = await ReadErrorAsync(response, "Error al actualizar la reserva", cancellationToken);
            var errorType = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
                System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
                System.Net.HttpStatusCode.Conflict => ServiceErrorType.Conflict,
                _ => ServiceErrorType.Validation
            };

            return ServiceResult.Fail(errorType, message);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Fail(ServiceErrorType.Unavailable, $"No se pudo comunicar con el servicio: {ex.Message}");
        }
    }

    // Spec 023 (RF-1 a RF-4): cancelación de una reserva pendiente con motivo obligatorio.
    public async Task<ServiceResult> CancelAsync(Guid id, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync(
                $"/api/maintenances/{id}/cancel",
                new CancelMaintenanceRequest { Reason = reason },
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return ServiceResult.Ok();
            }

            var (message, _) = await ReadErrorAsync(response, "Error al cancelar el mantenimiento", cancellationToken);
            var errorType = response.StatusCode switch
            {
                System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
                System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
                System.Net.HttpStatusCode.Conflict => ServiceErrorType.Conflict,
                _ => ServiceErrorType.Validation
            };

            return ServiceResult.Fail(errorType, message);
        }
        catch (HttpRequestException ex)
        {
            return ServiceResult.Fail(ServiceErrorType.Unavailable, $"No se pudo comunicar con el servicio: {ex.Message}");
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
