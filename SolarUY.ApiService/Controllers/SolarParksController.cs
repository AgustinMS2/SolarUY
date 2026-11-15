using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = Roles.Any)]
[ApiController]
[Route("api/solar-parks")]
public class SolarParksController : ControllerBase
{
    private readonly ISolarParkService _service;
    private readonly IParkHealthSnapshotBuilder _healthSnapshotBuilder;

    public SolarParksController(ISolarParkService service, IParkHealthSnapshotBuilder healthSnapshotBuilder)
    {
        _service = service;
        _healthSnapshotBuilder = healthSnapshotBuilder;
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPost]
    public IActionResult Create([FromBody] CreateSolarParkRequest request)
    {
        var result = _service.Create(request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<SolarParkDto>> GetAll()
    {
        var parks = _service.GetAll();
        return Ok(parks);
    }

    /// <summary>
    /// Carga inicial del mapa de salud de paneles del parque (spec 030, RF-1 y RF-3).
    /// </summary>
    [HttpGet("{id:guid}/panel-health")]
    public ActionResult<ParkHealthSnapshotDto> GetPanelHealth(Guid id)
    {
        var result = _healthSnapshotBuilder.BuildSnapshot(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<SolarParkDto> GetById(Guid id)
    {
        var result = _service.GetById(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateSolarParkRequest request)
    {
        var result = _service.Update(id, request);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return NoContent();
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpDelete("{id:guid}")]
    public IActionResult Deactivate(Guid id)
    {
        var result = _service.Deactivate(id);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return NoContent();
    }
}
