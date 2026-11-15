using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public interface ISolarUyLlmService
{
    Task<ChatQueryResponse> ProcessQueryAsync(string userMessage, IReadOnlyList<ChatMessageDto>? history = null, CancellationToken cancellationToken = default);
}

