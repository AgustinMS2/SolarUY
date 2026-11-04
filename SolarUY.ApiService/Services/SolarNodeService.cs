using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class SolarNodeService : ISolarNodeService
{
    private readonly ISolarNodeRepository _repository;
    private readonly ISolarParkRepository _parkRepository;
    private readonly ILogger<SolarNodeService> _logger;

    public SolarNodeService(ISolarNodeRepository repository, ISolarParkRepository parkRepository, ILogger<SolarNodeService> logger)
    {
        _repository = repository;
        _parkRepository = parkRepository;
        _logger = logger;
    }

    public ServiceResult<Guid> Create(CreateSolarNodeRequest request)
    {
        // Validar nombre vacío
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El nombre es obligatorio.");
        }

        // Trim y validar longitud
        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 100)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "El nombre no puede superar los 100 caracteres.");
        }

        var normalizedName = trimmedName.ToLowerInvariant();

        // Verificar que el parque exista y esté activo
        var park = _parkRepository.GetById(request.SolarParkId);
        if (park == null || park.Status != SolarParkStatus.Active)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.NotFound, "El parque solar seleccionado no existe o no está activo.");
        }

        // Verificar unicidad en el mismo parque
        if (_repository.ExistsByName(request.SolarParkId, normalizedName))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Conflict, $"Ya existe un nodo con el nombre \"{trimmedName}\" en este parque solar.");
        }

        var node = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = trimmedName,
            NormalizedName = normalizedName,
            SolarParkId = request.SolarParkId,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };

        _repository.Add(node);
        _logger.LogInformation("Solar node created successfully with ID {NodeId}", node.Id);

        return ServiceResult<Guid>.Ok(node.Id);
    }

    public IReadOnlyList<SolarNodeDto> GetAll(Guid? parkId = null)
    {
        var nodes = _repository.GetAll(parkId);

        return nodes.Select(ToDto).ToList().AsReadOnly();
    }

    public ServiceResult<SolarNodeDto> GetById(Guid id)
    {
        var node = _repository.GetById(id);
        if (node == null)
        {
            return ServiceResult<SolarNodeDto>.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        return ServiceResult<SolarNodeDto>.Ok(ToDto(node));
    }

    public ServiceResult Update(Guid id, UpdateSolarNodeRequest request)
    {
        var node = _repository.GetById(id);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El nombre es obligatorio.");
        }

        var trimmedName = request.Name.Trim();
        if (trimmedName.Length > 100)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El nombre no puede superar los 100 caracteres.");
        }

        var normalizedName = trimmedName.ToLowerInvariant();

        if (_repository.ExistsByName(node.SolarParkId, normalizedName, excludeId: id))
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, $"Ya existe un nodo con el nombre \"{trimmedName}\" en este parque solar.");
        }

        node.Name = trimmedName;
        node.NormalizedName = normalizedName;
        node.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(node);
        _logger.LogInformation("Solar node updated successfully with ID {NodeId}", node.Id);

        return ServiceResult.Ok();
    }

    private static SolarNodeDto ToDto(SolarNode node)
    {
        return new SolarNodeDto(
            node.Id,
            node.Name,
            node.SolarParkId,
            node.SolarPark?.Name ?? string.Empty,
            // Contar paneles relacionados - subconsulta simplificada
            0, // TODO: Implementar conteo de paneles cuando haya datos
            null, // InverterSerie - TODO: Cuándo haya inversores asignados
            node.Status == SolarNodeStatus.Active ? "Activo" : "Inactivo"
        );
    }
}