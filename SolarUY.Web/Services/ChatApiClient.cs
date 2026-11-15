using System.Net.Http.Json;

namespace SolarUY.Web.Services;

public record ChatMessageDto(
    string Role,
    string Content,
    DateTime TimestampUtc
);

public record ChatQueryRequest(
    string Message,
    List<ChatMessageDto>? History = null
);

public record ChatQueryResponse(
    string Answer,
    string? Source = null
);

public class ChatApiClient(HttpClient httpClient)
{
    public async Task<ChatQueryResponse?> SendMessageAsync(ChatQueryRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/chat/query", request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ChatQueryResponse>(cancellationToken: cancellationToken);
            }

            var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(cancellationToken: cancellationToken);
            return new ChatQueryResponse(error?.Message ?? "No se pudo obtener respuesta del asistente.", "Error");
        }
        catch (Exception ex)
        {
            return new ChatQueryResponse($"Error al comunicarse con el asistente: {ex.Message}", "NetworkError");
        }
    }

    private sealed record ErrorResponse(string? Message);
}

