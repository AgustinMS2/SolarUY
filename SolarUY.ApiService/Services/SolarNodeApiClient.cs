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
}