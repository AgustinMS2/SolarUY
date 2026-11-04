using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public class SolarNodeApiClient
{
    private readonly HttpClient _http;

    public SolarNodeApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateSolarNodeRequest request)
    {
        var response = await _http.PostAsync("api/solar-nodes", new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(request),
                    UnicodeEncoding.UTF8,
                    "application/json"));

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = System.Text.Json.JsonDocument.Parse(content);
            var id = doc.RootElement.GetProperty("id").GetGuid();
            return ServiceResult<Guid>.Ok(id);
        }

        var errorContent = await response.Content.ReadAsStringAsync();
        var errorDoc = System.Text.Json.JsonDocument.Parse(errorContent);
        var errorMessage = errorDoc.RootElement.GetProperty("message").GetString() ?? "Error desconocido";

        var errorType = response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
            System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            System.Net.HttpStatusCode.Conflict => ServiceErrorType.Conflict,
            _ => ServiceErrorType.Validation
        };

        return ServiceResult<Guid>.Fail(errorType, errorMessage);
    }

    public async Task<ServiceResult<IReadOnlyList<SolarNodeDto>>> GetAllAsync(Guid? parkId = null, CancellationToken cancellationToken = default)
    {
        var url = "/api/solar-nodes";
        if (parkId.HasValue)
        {
            url += $"?parkId={parkId.Value}";
        }

        var response = await _http.GetAsync(url, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var dtoList = System.Text.Json.JsonSerializer.Deserialize<IReadOnlyList<SolarNodeDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return ServiceResult<IReadOnlyList<SolarNodeDto>>.Ok(dtoList ?? new List<SolarNodeDto>());
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var errorDoc = System.Text.Json.JsonDocument.Parse(errorContent);
        var errorMessage = errorDoc.RootElement.GetProperty("message").GetString() ?? "Error desconocido";

        var errorType = response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
            System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            _ => ServiceErrorType.Validation
        };

        return ServiceResult<IReadOnlyList<SolarNodeDto>>.Fail(errorType, errorMessage);
    }

    public async Task<ServiceResult<SolarNodeDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"/api/solar-nodes/{id}", cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var dto = await response.Content.ReadFromJsonAsync<SolarNodeDto>(cancellationToken: cancellationToken);
            return dto != null
                ? ServiceResult<SolarNodeDto>.Ok(dto)
                : ServiceResult<SolarNodeDto>.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return ServiceResult<SolarNodeDto>.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);
        return ServiceResult<SolarNodeDto>.Fail(ServiceErrorType.Validation, errorMessage);
    }

    public async Task<ServiceResult> UpdateAsync(Guid id, UpdateSolarNodeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PutAsJsonAsync($"/api/solar-nodes/{id}", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return ServiceResult.Ok();
        }

        var errorMessage = await ReadErrorMessageAsync(response, cancellationToken);

        var errorType = response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
            System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            System.Net.HttpStatusCode.Conflict => ServiceErrorType.Conflict,
            _ => ServiceErrorType.Validation
        };

        return ServiceResult.Fail(errorType, errorMessage);
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(errorContent))
        {
            return $"Error al procesar la solicitud ({(int)response.StatusCode}).";
        }

        try
        {
            var errorDoc = System.Text.Json.JsonDocument.Parse(errorContent);
            if (errorDoc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString() ?? "Error desconocido";
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // La respuesta de error no es JSON; se usa el mensaje genérico.
        }

        return $"Error al procesar la solicitud ({(int)response.StatusCode}).";
    }
}