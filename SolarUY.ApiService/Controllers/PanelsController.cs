using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpDelete("{id:guid}")]
    public IActionResult DeletePanel(Guid id)
    {
        var result = _service.DeletePanel(id);

        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
            };
        }

        return Ok(new { message = result.ErrorMessage });
    }
}