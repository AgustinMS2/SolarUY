using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = Roles.Any)]
[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly ISolarUyLlmService _llmService;

    public ChatController(ISolarUyLlmService llmService)
    {
        _llmService = llmService;
    }

    /// <summary>
    /// Procesa una consulta en lenguaje natural sobre SolarUY utilizando el asistente inteligente (Spec 035).
    /// </summary>
    [Authorize(Roles = Roles.Any)]
    [HttpPost("query")]
    public async Task<ActionResult<ChatQueryResponse>> Query([FromBody] ChatQueryRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { message = "El mensaje no puede estar vacío." });
        }

        var response = await _llmService.ProcessQueryAsync(request.Message, request.History, cancellationToken);
        return Ok(response);
    }
}

