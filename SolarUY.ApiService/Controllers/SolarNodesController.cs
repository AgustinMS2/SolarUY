using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize]
[ApiController]
[Route("api/solar-nodes")]
public class SolarNodesController : ControllerBase
{
    private readonly ISolarNodeService _service;

    public SolarNodesController(ISolarNodeService service)
    {
        _service = service;
    }

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
}