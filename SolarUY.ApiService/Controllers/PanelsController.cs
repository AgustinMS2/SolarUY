using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize]
[ApiController]
[Route("api/panels")]
public class PanelsController : ControllerBase
{
    private readonly ISolarPanelService _service;

    public PanelsController(ISolarPanelService service)
    {
        _service = service;
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateSolarPanelRequest request)
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
    public ActionResult<IReadOnlyList<SolarPanelDto>> GetAll()
    {
        var panels = _service.GetAll();
        return Ok(panels);
    }

    [HttpGet("{id:guid}")]
    public ActionResult<SolarPanelDto> GetById(Guid id)
    {
        var result = _service.GetById(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.ErrorMessage });
        }

        return Ok(result.Value);
    }

    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id, [FromBody] UpdateSolarPanelRequest request)
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

        return Ok(new { message = "Panel actualizado correctamente." });
    }
}
