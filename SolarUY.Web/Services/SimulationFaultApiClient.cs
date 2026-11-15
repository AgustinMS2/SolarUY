using System.Net.Http.Json;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.Web.Services;

/// <summary>Resultado de una orden al simulador de fallas (spec 037): mensaje de éxito o de error de la API.</summary>
public class SimulationFaultResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>Cliente de la API del simulador de fallas (spec 037). Solo el Administrador accede.</summary>
public class SimulationFaultApiClient(HttpClient httpClient)
{
    private sealed record MessageResponse(string? Message);

    /// <summary>Equipos que emiten lecturas con su falla vigente, y si el simulador está habilitado en este entorno.</summary>
    public async Task<SimulationFaultOverviewDto?> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<SimulationFaultOverviewDto>("/api/simulation/faults", cancellationToken);
    }

    public Task<SimulationFaultResponse> InjectAsync(SimulatedFaultTarget targetType, Guid targetId, SimulatedFaultType faultType, CancellationToken cancellationToken = default)
    {
        return SendAsync(
            () => httpClient.PostAsJsonAsync(
                "/api/simulation/faults",
                new InjectSimulatedFaultRequest(targetType.ToString(), targetId, faultType.ToString()),
                cancellationToken),
            cancellationToken);
    }

    public Task<SimulationFaultResponse> ClearTargetAsync(SimulatedFaultTarget targetType, Guid targetId, CancellationToken cancellationToken = default)
    {
        return SendAsync(() => httpClient.DeleteAsync($"/api/simulation/faults/{targetType}/{targetId}", cancellationToken), cancellationToken);
    }

    public Task<SimulationFaultResponse> ClearAllAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(() => httpClient.DeleteAsync("/api/simulation/faults", cancellationToken), cancellationToken);
    }

    private static async Task<SimulationFaultResponse> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        try
        {
            var response = await send();
            var body = await ReadMessageAsync(response, cancellationToken);

            return response.IsSuccessStatusCode
                ? new SimulationFaultResponse { Success = true, Message = body }
                : new SimulationFaultResponse
                {
                    Success = false,
                    ErrorMessage = body ?? $"No se pudo completar la operación ({(int)response.StatusCode})."
                };
        }
        catch (Exception ex)
        {
            return new SimulationFaultResponse { Success = false, ErrorMessage = $"No se pudo comunicar con el servicio: {ex.Message}" };
        }
    }

    private static async Task<string?> ReadMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<MessageResponse>(cancellationToken: cancellationToken);
            return body?.Message;
        }
        catch (Exception)
        {
            // Respuestas sin cuerpo JSON (por ejemplo, 403).
            return null;
        }
    }
}
