using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

public class GeminiLlmService : ISolarUyLlmService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly SolarUyKnowledgeAssistant _localAssistant;
    private readonly ISolarUyDomainContextRetriever _retriever;
    private readonly ILogger<GeminiLlmService> _logger;

    public GeminiLlmService(
        IConfiguration configuration,
        HttpClient httpClient,
        SolarUyKnowledgeAssistant localAssistant,
        ISolarUyDomainContextRetriever retriever,
        ILogger<GeminiLlmService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _localAssistant = localAssistant;
        _retriever = retriever;
        _logger = logger;
    }

    public async Task<ChatQueryResponse> ProcessQueryAsync(
        string userMessage,
        IReadOnlyList<ChatMessageDto>? history = null,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return _localAssistant.ProcessQuery(userMessage, history);
        }

        try
        {
            var snapshot = _retriever.GetCurrentSnapshot();
            var contextJson = JsonSerializer.Serialize(snapshot);

            var systemInstructionText = "Eres el Asistente Inteligente de la plataforma SolarUY (Uruguay). " +
                "Tu objetivo es responder consultas de operadores sobre parques solares, generación de energía, alertas y mantenimientos en español, de forma clara, técnica y profesional. " +
                "Reglas estrictas:\n" +
                "1) Responde únicamente sobre la información de SolarUY y sus parques solares. Si el usuario pregunta cosas ajenas a la plataforma, responde: 'Solo puedo responder consultas relacionadas con la operación, generación, equipos y mantenimientos de los parques solares de SolarUY.'\n" +
                "2) Jamás reveles contraseñas, secretos, hashes ni tokens de Keycloak o de la base de datos.\n" +
                "3) Si la consulta requiere datos numéricos o comparativas, utiliza formato Markdown con tablas legibles.\n" +
                "Contexto actual del sistema:\n" + contextJson;

            var contents = new List<object>();

            if (history != null)
            {
                foreach (var h in history.TakeLast(6))
                {
                    contents.Add(new
                    {
                        role = h.Role == "user" ? "user" : "model",
                        parts = new[] { new { text = h.Content } }
                    });
                }
            }

            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemInstructionText } }
                },
                contents = contents
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                if (doc != null &&
                    doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0 &&
                    candidates[0].TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0 &&
                    parts[0].TryGetProperty("text", out var textElem))
                {
                    var answer = textElem.GetString();
                    if (!string.IsNullOrWhiteSpace(answer))
                    {
                        return new ChatQueryResponse(answer, "Google Gemini 2.5 Flash");
                    }
                }
            }

            _logger.LogWarning("La API de Gemini respondió con código {StatusCode}. Usando motor de conocimiento local.", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error comunicando con la API de Gemini. Recurriendo al asistente de dominio local.");
        }

        return _localAssistant.ProcessQuery(userMessage, history);
    }
}

