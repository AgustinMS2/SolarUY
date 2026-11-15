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

    public SolarParksController(
        ISolarParkService service,
        IParkHealthSnapshotBuilder healthSnapshotBuilder,
        IParkHistoricalGenerationService historicalService)
    {
        _service = service;
        _healthSnapshotBuilder = healthSnapshotBuilder;
        _historicalService = historicalService;
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
}
