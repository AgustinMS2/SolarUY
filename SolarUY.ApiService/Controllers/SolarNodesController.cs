using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = Roles.Any)]
[ApiController]
[Route("api/solar-nodes")]
public class SolarNodesController : ControllerBase
{
    private readonly ISolarNodeService _service;
    private readonly ISolarPanelService _panelService;
    private readonly IGridInjectionService _injectionService;

    public SolarNodesController(ISolarNodeService service, ISolarPanelService panelService, IGridInjectionService injectionService)
    {
        _service = service;
        _panelService = panelService;
        _injectionService = injectionService;
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPost]
    public IActionResult Create([FromBody] CreateSolarNodeRequest request)
    {
        var result = _service.Create(request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [HttpGet]
    public IActionResult GetAll(Guid? parkId = null)
    {
        var result = _service.GetAll(parkId);

        if (result == null || result.Count == 0)
        {
            return Ok(new List<SolarNodeDto>());
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        var result = _service.GetById(id);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(result.Value);
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateSolarNodeRequest request)
    {
        var result = _service.Update(id, request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        var result = _service.Deactivate(id);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/inverter")]
    public IActionResult AssignInverter(Guid id, [FromBody] AssignInverterRequest request)
    {
        var result = _service.AssignInverter(id, request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                // 409 as well, but flagged so the screen can ask the user to confirm the replacement.
                ServiceErrorType.ConfirmationRequired => Conflict(new { message = result.ErrorMessage, requiresConfirmation = true }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(new { message = "Inversor asignado correctamente al nodo." });
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpDelete("{id:guid}/inverter")]
    public IActionResult UnassignInverter(Guid id)
    {
        var result = _service.UnassignInverter(id);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/panels")]
    public IActionResult AssignPanels(Guid id, [FromBody] AssignPanelsRequest request)
    {
        var result = _panelService.AssignPanelsToNode(id, request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                // 409 flagged so the screen can ask the Administrator to confirm the warnings (reassignment / power).
                ServiceErrorType.ConfirmationRequired => Conflict(new { message = result.ErrorMessage, requiresConfirmation = true }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(new { message = "Paneles asignados correctamente al nodo." });
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpDelete("{id:guid}/panels")]
    public IActionResult UnassignPanels(Guid id, [FromBody] UnassignPanelsRequest request)
    {
        var result = _panelService.UnassignPanelsFromNode(id, request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(new { message = "Paneles desasociados correctamente del nodo." });
    }

    // Spec 033 (caso límite): corte de la inyección del inversor individual del nodo; solo Administradores (RF-6).
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/inverter/cut-injection")]
    public IActionResult CutInverterInjection(Guid id, [FromBody] CutInjectionRequest request)
    {
        var result = _injectionService.CutInverterInjection(id, request.ConfirmationText, GetOrderedBy(), GetRemoteIp(), request.Reason);
        return InjectionResponse(result, "Detenida");
    }

    // Spec 033 (caso límite): reanudación de la inyección del inversor individual del nodo; solo Administradores (RF-6).
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/inverter/resume-injection")]
    public IActionResult ResumeInverterInjection(Guid id, [FromBody] CutInjectionRequest request)
    {
        var result = _injectionService.ResumeInverterInjection(id, request.ConfirmationText, GetOrderedBy(), GetRemoteIp(), request.Reason);
        return InjectionResponse(result, "Activa");
    }

    // Spec 033: traduce el resultado del servicio a la respuesta HTTP (el cuerpo siempre lleva "message" en los errores).
    private IActionResult InjectionResponse(ServiceResult result, string status)
    {
        if (result.Success)
        {
            return Ok(new { status });
        }

        return result.ErrorType switch
        {
            ServiceErrorType.Validation or ServiceErrorType.ConfirmationRequired => BadRequest(new { message = result.ErrorMessage }),
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            // Caso límite: pérdida de comunicación con el inversor.
            ServiceErrorType.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
        };
    }

    // Bitácora (spec 033, RF-6): usuario autenticado que ordena la acción.
    private string GetOrderedBy()
    {
        return User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.Identity?.Name
            ?? "desconocido";
    }

    private string? GetRemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
