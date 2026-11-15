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
    private readonly IParkHistoricalGenerationService _historicalService;
    private readonly IGridInjectionService _injectionService;

    public SolarParksController(
        ISolarParkService service,
        IParkHealthSnapshotBuilder healthSnapshotBuilder,
        IParkHistoricalGenerationService historicalService,
        IGridInjectionService injectionService)
    {
        _service = service;
        _healthSnapshotBuilder = healthSnapshotBuilder;
        _historicalService = historicalService;
        _injectionService = injectionService;
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

    /// <summary>
    /// Consulta de generación histórica diaria por horas (Spec 034, RF-2).
    /// </summary>
    [HttpGet("{id:guid}/history/daily")]
    public ActionResult<HistoricalGenerationReportDto> GetDailyHistory(Guid id, [FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));
        var result = _historicalService.GetDailyReport(id, targetDate);
        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Consulta de generación histórica mensual por días (Spec 034, RF-3).
    /// </summary>
    [HttpGet("{id:guid}/history/monthly")]
    public ActionResult<HistoricalGenerationReportDto> GetMonthlyHistory(Guid id, [FromQuery] int? year, [FromQuery] int? month)
    {
        var now = DateTime.UtcNow.AddHours(-3);
        var targetYear = year ?? now.Year;
        var targetMonth = month ?? now.Month;

        var result = _historicalService.GetMonthlyReport(id, targetYear, targetMonth);
        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Consulta de generación histórica anual por meses (Spec 034, RF-4).
    /// </summary>
    [HttpGet("{id:guid}/history/yearly")]
    public ActionResult<HistoricalGenerationReportDto> GetYearlyHistory(Guid id, [FromQuery] int? year)
    {
        var targetYear = year ?? DateTime.UtcNow.AddHours(-3).Year;

        var result = _historicalService.GetYearlyReport(id, targetYear);
        if (!result.Success)
        {
            return result.ErrorType switch
            {
                ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
                _ => BadRequest(new { message = result.ErrorMessage })
            };
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

    // Spec 033 (RF-1/RF-2): corte administrativo de la inyección a la red de todo el parque; solo Administradores (RF-6).
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/cut-injection")]
    public IActionResult CutInjection(Guid id, [FromBody] CutInjectionRequest request)
    {
        var result = _injectionService.CutParkInjection(id, request.ConfirmationText, GetOrderedBy(), GetRemoteIp(), request.Reason);
        return InjectionResponse(result, "Detenida");
    }

    // Spec 033 (RF-5): reanudación de la inyección a la red del parque; solo Administradores (RF-6).
    [Authorize(Roles = Roles.Administrator)]
    [HttpPost("{id:guid}/resume-injection")]
    public IActionResult ResumeInjection(Guid id, [FromBody] CutInjectionRequest request)
    {
        var result = _injectionService.ResumeParkInjection(id, request.ConfirmationText, GetOrderedBy(), GetRemoteIp(), request.Reason);
        return InjectionResponse(result, "Activa");
    }

    // Spec 033: traduce el resultado del servicio a la respuesta HTTP (el cuerpo siempre lleva "message" en los errores).
    private IActionResult InjectionResponse(ServiceResult result, string status)
    {
        if (result.Success)
        {
            return Ok(new { status });
        }

        return result.ErrorType switch
        {
            ServiceErrorType.Validation or ServiceErrorType.ConfirmationRequired => BadRequest(new { message = result.ErrorMessage }),
            ServiceErrorType.NotFound => NotFound(new { message = result.ErrorMessage }),
            ServiceErrorType.Conflict => Conflict(new { message = result.ErrorMessage }),
            // Caso límite: pérdida de comunicación con el inversor.
            ServiceErrorType.Unavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = result.ErrorMessage }),
            _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = result.ErrorMessage })
        };
    }

    // Bitácora (spec 033, RF-6): usuario autenticado que ordena la acción.
    private string GetOrderedBy()
    {
        return User.FindFirst("preferred_username")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? User.Identity?.Name
            ?? "desconocido";
    }

    private string? GetRemoteIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
