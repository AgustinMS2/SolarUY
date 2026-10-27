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

    public ServiceResult<Guid> Create(CreateSolarParkRequest request)
    {
        // 1. Validaciones de RF-2
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El nombre es obligatorio.");
        }

        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 100)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El nombre no puede superar los 100 caracteres.");
        }

        if (request.Latitude < -90.0 || request.Latitude > 90.0)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "La latitud debe estar entre -90 y 90.");
        }

        if (request.Longitude < -180.0 || request.Longitude > 180.0)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "La longitud debe estar entre -180 y 180.");
        }

        if (request.MaxCapacityKw <= 0)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "La capacidad máxima debe ser mayor a 0.");
        }

        if (request.PowerAlertThresholdKw <= 0 || request.PowerAlertThresholdKw > request.MaxCapacityKw)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El umbral de alerta debe ser mayor a 0 y no puede superar la capacidad máxima.");
        }

        // 2. Unicidad de nombre (RF-3)
        var normalizedName = trimmedName.ToLowerInvariant();
        if (_repository.ExistsByNormalizedName(normalizedName))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Conflict, $"Ya existe un parque con el nombre \"{trimmedName}\".");
        }

        // 3. Creación y persistencia (RF-1)
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
}
