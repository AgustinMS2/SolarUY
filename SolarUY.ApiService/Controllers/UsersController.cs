using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Security;
using SolarUY.ApiService.Services;

namespace SolarUY.ApiService.Controllers;

[Authorize(Roles = Roles.Administrator)]
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserRoleService _service;

    public UsersController(IUserRoleService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var result = _service.GetAll();

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return Ok(result.Value);
    }

    [Authorize(Roles = Roles.Administrator)]
    [HttpPut("{id}/role")]
    public IActionResult ChangeRole(string id, [FromBody] ChangeUserRoleRequest request)
    {
        var result = _service.ChangeRole(id, request);

        if (!result.Success)
        {
            return ToErrorResponse(result);
        }

        return NoContent();
    }

    private IActionResult ToErrorResponse(ServiceResult result)
    {
        return result.ErrorType switch
        {
            ServiceErrorType.Validation => BadRequest(new { message = result.ErrorMessage }),
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            ServiceErrorType.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
        };
    }
}
