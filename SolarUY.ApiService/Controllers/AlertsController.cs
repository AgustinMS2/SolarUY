using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = Roles.Any)]
[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private const int MaxTake = 200;

    private readonly IAlertService _service;
    private readonly IAlertPublisher _publisher;

    public AlertsController(IAlertService service, IAlertPublisher publisher)
    {
        _service = service;
        _publisher = publisher;
    }

    /// <summary>
    /// Alertas recientes y contador de Pendientes para la campana y el centro de alertas (spec 031, RF-3).
    /// </summary>
    [HttpGet]
    public ActionResult<AlertListDto> GetRecent([FromQuery] int take = 50)
    {
        var limited = Math.Clamp(take, 1, MaxTake);
        return Ok(_service.GetRecent(limited));
    }

    /// <summary>
    /// Reconoce una alerta: la pasa de Pendiente a Reconocida para todos los usuarios (spec 031, RF-4).
    /// Cualquier rol puede reconocer; la excepción figura en los tests de seguridad.
    /// </summary>
    [Authorize(Roles = Roles.Any)]
    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = _service.Acknowledge(id);
        return await ToResponse(result, cancellationToken);
    }

    /// <summary>
    /// Descarta una alerta Reconocida (spec 031, RF-7). Solo el Administrador.
    /// </summary>
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid id, CancellationToken cancellationToken)
    {
        var result = _service.Dismiss(id);
        return await ToResponse(result, cancellationToken);
    }

    private async Task<IActionResult> ToResponse(ServiceResult<AlertDto> result, CancellationToken cancellationToken)
    {
        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        // Estado global: todos los usuarios conectados actualizan su campana y su lista.
        await _publisher.PublishStatusChangeAsync(result.Value!, cancellationToken);
        return Ok(result.Value);
    }
}
