using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = Roles.Any)]
[ApiController]
[Route("api/maintenances")]
public class MaintenancesController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenancesController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPost]
    public IActionResult CreateReservation([FromBody] CreateMaintenanceReservationRequest request)
    {
        var result = _maintenanceService.CreateReservation(request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                ServiceErrorType.ConfirmationRequired => Conflict(new { message = result.ErrorMessage, requiresConfirmation = true }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return CreatedAtAction(nameof(CreateReservation), new { id = result.Value }, new { id = result.Value });
    }

    // Listado con filtros combinables (spec 024, RF-1 a RF-4).
    [HttpGet]
    public IActionResult GetAll([FromQuery] MaintenanceFilter filter)
    {
        var result = _maintenanceService.GetAll(filter);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(result.Value);
    }

    // Órdenes asignadas al técnico autenticado (spec 025, RF-1).
    [Authorize(Roles = Roles.Any)]
    [HttpGet("mine")]
    public IActionResult GetMyMaintenances([FromQuery] string? email = null)
    {
        string? targetEmail = null;
        if (User.IsInRole(Roles.Administrator) && !string.IsNullOrWhiteSpace(email))
        {
            targetEmail = email;
        }
        else
        {
            targetEmail = User.FindFirst("email")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("preferred_username")?.Value;
        }

        if (string.IsNullOrWhiteSpace(targetEmail))
        {
            return BadRequest(new { message = "No se pudo determinar el correo del usuario autenticado." });
        }

        var result = _maintenanceService.GetMyMaintenances(targetEmail);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(result.Value);
    }

    // Precarga de la edición de una reserva (spec 022, RF-1).
    [HttpGet("{id:guid}")]
    public IActionResult GetOrder(Guid id)
    {
        var result = _maintenanceService.GetById(id);

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

    // Modificación de una reserva pendiente (spec 022, RF-2 a RF-4).
    [Authorize(Roles = Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public IActionResult UpdateReservation(Guid id, [FromBody] UpdateMaintenanceReservationRequest request)
    {
        var result = _maintenanceService.UpdateReservation(id, request);

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

    // Cancelación de una reserva pendiente con motivo obligatorio (spec 023, RF-1 a RF-4).
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/cancel")]
    public IActionResult CancelReservation(Guid id, [FromBody] CancelMaintenanceRequest request)
    {
        var result = _maintenanceService.CancelReservation(id, request);

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

        return Ok(new { message = "Mantenimiento cancelado correctamente." });
    }
}
