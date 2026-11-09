using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/technicians")]
public class TechniciansController : ControllerBase
{
    private readonly ITechnicianService _technicianService;

    public TechniciansController(ITechnicianService technicianService)
    {
        _technicianService = technicianService;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateTechnicianRequest request)
    {
        var result = _technicianService.Create(request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value }, new { id = result.Value });
    }
}