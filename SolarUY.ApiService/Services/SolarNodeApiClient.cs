using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public class SolarNodeApiClient
{
    private readonly HttpClient _http;

    public SolarNodeApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<ServiceResult<Guid>> CreateAsync(CreateSolarNodeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("/api/solar-nodes", request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var doc = JsonDocument.Parse(content);
            var id = doc.RootElement.GetProperty("id").GetGuid();
            return ServiceResult<Guid>.Ok(id);
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        string errorMessage;
        try
        {
            var errorDoc = JsonDocument.Parse(errorContent);
            errorMessage = errorDoc.RootElement.GetProperty("message").GetString() ?? "Error al registrar el nodo";
        }
        catch
        {
            errorMessage = $"Error al registrar el nodo ({response.StatusCode}).";
        }

        var errorType = response.StatusCode switch
        {
            System.Net.HttpStatusCode.Conflict => ServiceErrorType.Conflict,
            System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
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
            var dtoList = JsonSerializer.Deserialize<IReadOnlyList<SolarNodeDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return ServiceResult<IReadOnlyList<SolarNodeDto>>.Ok(dtoList ?? new List<SolarNodeDto>());
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        string errorMessage;
        try
        {
            var errorDoc = JsonDocument.Parse(errorContent);
            errorMessage = errorDoc.RootElement.GetProperty("message").GetString() ?? "Error desconocido";
        }
        catch
        {
            errorMessage = "Error desconocido";
        }

        var errorType = response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => ServiceErrorType.Validation,
            System.Net.HttpStatusCode.NotFound => ServiceErrorType.NotFound,
            _ => ServiceErrorType.Validation
        };

        return ServiceResult<IReadOnlyList<SolarNodeDto>>.Fail(errorType, errorMessage);
    }
}