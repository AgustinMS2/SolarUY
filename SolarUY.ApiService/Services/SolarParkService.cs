using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class SolarParkService : ISolarParkService
{
    private readonly ISolarParkRepository _repository;
    private readonly ILogger<SolarParkService> _logger;

    public SolarParkService(ISolarParkRepository repository, ILogger<SolarParkService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    private static ServiceResult? Validate(
        string? name,
        double latitude,
        double longitude,
        double maxCapacityKw,
        double powerAlertThresholdKw,
        out string trimmedName,
        out string normalizedName)
    {
        trimmedName = string.Empty;
        normalizedName = string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El nombre es obligatorio.");
        }

        trimmedName = name.Trim();
        if (trimmedName.Length > 100)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El nombre no puede superar los 100 caracteres.");
        }

        if (latitude < -90.0 || latitude > 90.0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La latitud debe estar entre -90 y 90.");
        }

        if (longitude < -180.0 || longitude > 180.0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La longitud debe estar entre -180 y 180.");
        }

        if (maxCapacityKw <= 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La capacidad máxima debe ser mayor a 0.");
        }

        if (powerAlertThresholdKw <= 0 || powerAlertThresholdKw > maxCapacityKw)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El umbral de alerta debe ser mayor a 0 y no puede superar la capacidad máxima.");
        }

        normalizedName = trimmedName.ToLowerInvariant();
        return null;
    }

    public ServiceResult<Guid> Create(CreateSolarParkRequest request)
    {
        var validationError = Validate(
            request.Name,
            request.Latitude,
            request.Longitude,
            request.MaxCapacityKw,
            request.PowerAlertThresholdKw,
            out var trimmedName,
            out var normalizedName);

        if (validationError != null)
        {
            return ServiceResult<Guid>.Fail(validationError.ErrorType, validationError.ErrorMessage!);
        }

        if (_repository.ExistsByNormalizedName(normalizedName))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Conflict, $"Ya existe un parque con el nombre \"{trimmedName}\".");
        }

        var park = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            NormalizedName = normalizedName,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            MaxCapacityKw = request.MaxCapacityKw,
            PowerAlertThresholdKw = request.PowerAlertThresholdKw,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.Add(park);
        _logger.LogInformation("Solar park created successfully with ID {ParkId}", park.Id);

        return ServiceResult<Guid>.Ok(park.Id);
    }

    public IReadOnlyList<SolarParkDto> GetAll()
    {
        var parks = _repository.GetAll();

        return parks
            .OrderBy(p => p.NormalizedName, StringComparer.OrdinalIgnoreCase)
            .Select(p => new SolarParkDto(
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude,
                p.MaxCapacityKw,
                p.PowerAlertThresholdKw,
                p.Status == SolarParkStatus.Active ? "Activo" : "Inactivo"
            ))
            .ToList();
    }

    public ServiceResult<SolarParkDto> GetById(Guid id)
    {
        var park = _repository.GetById(id);
        if (park == null)
        {
            return ServiceResult<SolarParkDto>.Fail(ServiceErrorType.NotFound, "El parque solicitado no existe.");
        }

        var dto = new SolarParkDto(
            park.Id,
            park.Name,
            park.Latitude,
            park.Longitude,
            park.MaxCapacityKw,
            park.PowerAlertThresholdKw,
            park.Status == SolarParkStatus.Active ? "Activo" : "Inactivo"
        );

        return ServiceResult<SolarParkDto>.Ok(dto);
    }

    public ServiceResult Update(Guid id, UpdateSolarParkRequest request)
    {
        var park = _repository.GetById(id);
        if (park == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El parque solicitado no existe.");
        }

        var validationError = Validate(
            request.Name,
            request.Latitude,
            request.Longitude,
            request.MaxCapacityKw,
            request.PowerAlertThresholdKw,
            out var trimmedName,
            out var normalizedName);

        if (validationError != null)
        {
            return validationError;
        }

        if (_repository.ExistsByNormalizedName(normalizedName, excludeId: id))
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, $"Ya existe un parque con el nombre \"{trimmedName}\".");
        }

        park.Name = trimmedName;
        park.NormalizedName = normalizedName;
        park.Latitude = request.Latitude;
        park.Longitude = request.Longitude;
        park.MaxCapacityKw = request.MaxCapacityKw;
        park.PowerAlertThresholdKw = request.PowerAlertThresholdKw;
        park.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(park);
        _logger.LogInformation("Solar park updated successfully with ID {ParkId}", park.Id);

        return ServiceResult.Ok();
    }

    public ServiceResult Deactivate(Guid id)
    {
        var park = _repository.GetById(id);
        if (park == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El parque solicitado no existe.");
        }

        if (park.Status == SolarParkStatus.Inactive)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "El parque ya está inactivo.");
        }

        park.Status = SolarParkStatus.Inactive;
        park.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(park);
        _logger.LogInformation("Solar park deactivated successfully with ID {ParkId}", park.Id);

        return ServiceResult.Ok();
    }
}
