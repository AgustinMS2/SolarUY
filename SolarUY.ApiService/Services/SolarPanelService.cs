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

    private static ServiceResult? Validate(
        string? serieNumber,
        int powerW,
        double efficiency,
        Guid solarParkId,
        ISolarPanelRepository repository,
        out string trimmedSerie,
        out string normalizedSerie)
    {
        trimmedSerie = string.Empty;
        normalizedSerie = string.Empty;

        if (string.IsNullOrWhiteSpace(serieNumber))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El número de serie es obligatorio.");
        }

        trimmedSerie = serieNumber.Trim();
        if (trimmedSerie.Length > 50)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El número de serie no puede superar los 50 caracteres.");
        }

        if (powerW <= 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La potencia debe ser mayor a 0.");
        }

        if (efficiency <= 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La eficiencia debe ser mayor a 0.");
        }

        if (efficiency > 100)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La eficiencia no puede superar el 100.");
        }

        var park = repository.GetParkById(solarParkId);
        if (park == null || park.Status != SolarParkStatus.Active)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El parque solar seleccionado no existe o no está activo.");
        }

        normalizedSerie = trimmedSerie.ToLowerInvariant();
        return null;
    }

    public ServiceResult<Guid> Create(CreateSolarPanelRequest request)
    {
        var validationError = Validate(
            request.SerieNumber,
            request.PowerW,
            request.Efficiency,
            request.SolarParkId,
            _repository,
            out var trimmedSerie,
            out var normalizedSerie);

        if (validationError != null)
        {
            return ServiceResult<Guid>.Fail(validationError.ErrorType, validationError.ErrorMessage!);
        }

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
                p.Status == SolarPanelStatus.Active ? "Activo" : "Inactivo",
                p.SolarParkId
            ))
            .ToList();
    }

    public ServiceResult<SolarPanelDto> GetById(Guid id)
    {
        var panel = _repository.GetById(id);
        if (panel == null)
        {
            return ServiceResult<SolarPanelDto>.Fail(ServiceErrorType.NotFound, "El panel solicitado no existe.");
        }

        var dto = new SolarPanelDto(
            panel.Id,
            panel.SerieNumber,
            panel.PowerW,
            panel.Efficiency,
            panel.SolarPark?.Name ?? string.Empty,
            panel.Status == SolarPanelStatus.Active ? "Activo" : "Inactivo",
            panel.SolarParkId
        );

        return ServiceResult<SolarPanelDto>.Ok(dto);
    }

    public ServiceResult Update(Guid id, UpdateSolarPanelRequest request)
    {
        var panel = _repository.GetById(id);
        if (panel == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El panel solicitado no existe.");
        }

        var validationError = Validate(
            request.SerieNumber,
            request.PowerW,
            request.Efficiency,
            request.SolarParkId,
            _repository,
            out var trimmedSerie,
            out var normalizedSerie);

        if (validationError != null)
        {
            return validationError;
        }

        if (_repository.ExistsBySerieNumber(normalizedSerie, excludeId: id))
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, $"Ya existe un panel con el número de serie \"{trimmedSerie}\".");
        }

        panel.SerieNumber = trimmedSerie;
        panel.NormalizedSerieNumber = normalizedSerie;
        panel.PowerW = request.PowerW;
        panel.Efficiency = request.Efficiency;
        panel.SolarParkId = request.SolarParkId;
        panel.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(panel);
        _logger.LogInformation("Solar panel updated successfully with ID {PanelId}", panel.Id);

        return ServiceResult.Ok();
    }
}
