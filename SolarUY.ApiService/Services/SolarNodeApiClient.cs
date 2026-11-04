using System.Net.Http;
using System.Net.Http.Headers;
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
            // Deserializar el ID del response 201 Created
            var doc = System.Text.Json.JsonDocument.Parse(content);
            var id = doc.RootElement.GetProperty("id").GetGuid();
            return ServiceResult<Guid>.Ok(id);
        }

        // Manejar errores conocidos
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
}