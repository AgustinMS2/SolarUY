using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

/// <summary>
/// Simulador de fallas (spec 037): inyecta y restablece fallas en paneles e inversores para probar las alertas y el
/// mantenimiento de punta a punta. Solo para el Administrador (RF-8).
/// </summary>
[Authorize(Roles = Roles.Administrator)]
[ApiController]
[Route("api/simulation/faults")]
public class SimulationFaultsController : ControllerBase
{
    private readonly ISimulationFaultService _service;

    public SimulationFaultsController(ISimulationFaultService service)
    {
        _service = service;
    }

    /// <summary>Equipos que emiten lecturas, con su falla simulada vigente, y si el simulador está habilitado.</summary>
    [HttpGet]
    public IActionResult GetOverview()
    {
        return Ok(_service.GetOverview());
    }

    // RF-1: inyecta una falla en un panel o en el inversor de un nodo.
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost]
    public IActionResult Inject([FromBody] InjectSimulatedFaultRequest request)
    {
        if (!Enum.TryParse<SimulatedFaultTarget>(request.TargetType, ignoreCase: true, out var targetType) || !Enum.IsDefined(targetType))
        {
            return BadRequest(new { message = "El tipo de equipo debe ser Panel o Inverter." });
        }

        if (!Enum.TryParse<SimulatedFaultType>(request.FaultType, ignoreCase: true, out var faultType) || !Enum.IsDefined(faultType))
        {
            return BadRequest(new { message = "El tipo de falla no es válido." });
        }

        return ToResponse(_service.Inject(targetType, request.TargetId, faultType, GetOrderedBy()));
    }

    // RF-4: "Restablecer / Reparar Falla Simulada" de un equipo.
    [Authorize(Roles = Roles.Administrator)]
    [HttpDelete("{targetType}/{targetId:guid}")]
    public IActionResult ClearTarget(string targetType, Guid targetId)
    {
        if (!Enum.TryParse<SimulatedFaultTarget>(targetType, ignoreCase: true, out var target) || !Enum.IsDefined(target))
        {
            return BadRequest(new { message = "El tipo de equipo debe ser Panel o Inverter." });
        }

        return ToResponse(_service.ClearTarget(target, targetId));
    }

    // RF-5: "Restablecer todas las fallas".
    [Authorize(Roles = Roles.Administrator)]
    [HttpDelete]
    public IActionResult ClearAll()
    {
        return ToResponse(_service.ClearAll());
    }

    private IActionResult ToResponse(ServiceResult<string> result)
    {
        if (result.Success)
        {
            return Ok(new { message = result.Value });
        }

        return result.ErrorType switch
        {
            ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            // El simulador está deshabilitado en este entorno (RF-6).
            ServiceErrorType.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
        };
    }

    // Bitácora: usuario autenticado que inyecta la falla.
    private string GetOrderedBy()
    {
        return User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.Identity?.Name
            ?? "desconocido";
    }
}
