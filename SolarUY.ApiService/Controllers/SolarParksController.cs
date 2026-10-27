using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[ApiController]
[Route("api/solar-parks")]
public class SolarParksController : ControllerBase
{
    private readonly ISolarParkService _service;

    public SolarParksController(ISolarParkService service)
    {
        _service = service;
    }

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
}
