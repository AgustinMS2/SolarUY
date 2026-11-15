namespace SolarUY.ApiService.Dtos;

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

