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
}
