using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class SolarPanelService : ISolarPanelService
{
    private readonly ISolarPanelRepository _repository;
    private readonly ILogger<SolarPanelService> _logger;

    public SolarPanelService(ISolarPanelRepository repository, ILogger<SolarPanelService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public ServiceResult<Guid> Create(CreateSolarPanelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SerieNumber))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El número de serie es obligatorio.");
        }

        var trimmedSerie = request.SerieNumber.Trim();
        if (trimmedSerie.Length > 50)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El número de serie no puede superar los 50 caracteres.");
        }

        if (request.PowerW <= 0)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "La potencia debe ser mayor a 0.");
        }

        if (request.Efficiency <= 0)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "La eficiencia debe ser mayor a 0.");
        }

        if (request.Efficiency > 100)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "La eficiencia no puede superar el 100.");
        }

        var park = _repository.GetParkById(request.SolarParkId);
        if (park == null || park.Status != SolarParkStatus.Active)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El parque solar seleccionado no existe o no está activo.");
        }

        var normalizedSerie = trimmedSerie.ToLowerInvariant();
        if (_repository.ExistsBySerieNumber(normalizedSerie))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Conflict, $"Ya existe un panel con el número de serie \"{trimmedSerie}\".");
        }

        var panel = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = trimmedSerie,
            NormalizedSerieNumber = normalizedSerie,
            PowerW = request.PowerW,
            Efficiency = request.Efficiency,
            SolarParkId = request.SolarParkId,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.Add(panel);
        _logger.LogInformation("Solar panel created successfully with ID {PanelId}", panel.Id);

        return ServiceResult<Guid>.Ok(panel.Id);
    }

    public IReadOnlyList<SolarPanelDto> GetAll()
    {
        var panels = _repository.GetAll();

        return panels
            .OrderBy(p => p.NormalizedSerieNumber, StringComparer.OrdinalIgnoreCase)
            .Select(p => new SolarPanelDto(
                p.Id,
                p.SerieNumber,
                p.PowerW,
                p.Efficiency,
                p.SolarPark?.Name ?? string.Empty,
                p.Status == SolarPanelStatus.Active ? "Activo" : "Inactivo"
            ))
            .ToList();
    }
}
