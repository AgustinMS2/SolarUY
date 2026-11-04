using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class SolarNodeService : ISolarNodeService
{
    private readonly ISolarNodeRepository _repository;
    private readonly ISolarParkRepository _parkRepository;
    private readonly IInverterRepository _inverterRepository;
    private readonly ISolarPanelRepository _panelRepository;
    private readonly ILogger<SolarNodeService> _logger;

    public SolarNodeService(
        ISolarNodeRepository repository,
        ISolarParkRepository parkRepository,
        IInverterRepository inverterRepository,
        ISolarPanelRepository panelRepository,
        ILogger<SolarNodeService> logger)
    {
        _repository = repository;
        _parkRepository = parkRepository;
        _inverterRepository = inverterRepository;
        _panelRepository = panelRepository;
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

        // One query for every node's inverter instead of one per row.
        var nodeIds = nodes.Select(n => n.Id).ToList();
        var serieByNode = _inverterRepository
            .GetByNodeIds(nodeIds)
            .ToDictionary(i => i.SolarNodeId, i => i.SerieNumber);
        var panelCountByNode = _panelRepository.CountByNodeIds(nodeIds);

        return nodes
            .Select(n => ToDto(n, serieByNode.GetValueOrDefault(n.Id), panelCountByNode.GetValueOrDefault(n.Id)))
            .ToList()
            .AsReadOnly();
    }

    public ServiceResult<SolarNodeDto> GetById(Guid id)
    {
        var node = _repository.GetById(id);
        if (node == null)
        {
            return ServiceResult<SolarNodeDto>.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var panelsCount = _panelRepository.CountByNodeIds([node.Id]).GetValueOrDefault(node.Id);
        return ServiceResult<SolarNodeDto>.Ok(ToDto(node, _inverterRepository.GetByNodeId(node.Id)?.SerieNumber, panelsCount));
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

    private static SolarNodeDto ToDto(SolarNode node, string? inverterSerie, int panelsCount)
    {
        return new SolarNodeDto(
            node.Id,
            node.Name,
            node.SolarParkId,
            node.SolarPark?.Name ?? string.Empty,
            panelsCount,
            inverterSerie,
            node.Status == SolarNodeStatus.Active ? "Activo" : "Inactivo"
        );
    }

    public ServiceResult AssignInverter(Guid nodeId, AssignInverterRequest request)
    {
        var node = _repository.GetById(nodeId);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        if (node.Status == SolarNodeStatus.Inactive)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "No se puede asignar un inversor a un nodo inactivo.");
        }

        var serie = request.SerieNumber?.Trim() ?? string.Empty;
        var brandAndModel = request.BrandAndModel?.Trim() ?? string.Empty;
        var endpoint = string.IsNullOrWhiteSpace(request.Endpoint) ? null : request.Endpoint.Trim();

        if (serie.Length == 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El número de serie es obligatorio.");
        }

        if (serie.Length > 50)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El número de serie no puede superar los 50 caracteres.");
        }

        if (brandAndModel.Length == 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La marca y modelo son obligatorios.");
        }

        if (brandAndModel.Length > 100)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La marca y modelo no pueden superar los 100 caracteres.");
        }

        if (endpoint != null && endpoint.Length > 100)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La dirección o endpoint no puede superar los 100 caracteres.");
        }

        if (request.NominalCapacityKw <= 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La capacidad nominal del inversor debe ser mayor a 0.");
        }

        var park = _parkRepository.GetById(node.SolarParkId);
        if (park == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El parque solar del nodo no existe.");
        }

        if (request.NominalCapacityKw > park.MaxCapacityKw)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La capacidad nominal del inversor no puede superar la capacidad máxima del parque solar.");
        }

        var normalizedSerie = serie.ToLowerInvariant();

        var inverterWithSameSerie = _inverterRepository.GetBySerie(normalizedSerie);
        if (inverterWithSameSerie != null && inverterWithSameSerie.SolarNodeId != node.Id)
        {
            var otherNodeName = _repository.GetById(inverterWithSameSerie.SolarNodeId)?.Name ?? string.Empty;
            return ServiceResult.Fail(
                ServiceErrorType.Conflict,
                $"El inversor con número de serie \"{serie}\" ya se encuentra asignado al nodo \"{otherNodeName}\".");
        }

        var currentInverter = _inverterRepository.GetByNodeId(node.Id);
        if (currentInverter != null && currentInverter.NormalizedSerieNumber != normalizedSerie && !request.ConfirmReplace)
        {
            return ServiceResult.Fail(
                ServiceErrorType.ConfirmationRequired,
                $"El nodo ya tiene el inversor \"{currentInverter.SerieNumber}\". ¿Deseás reemplazarlo por \"{serie}\"?");
        }

        if (currentInverter != null)
        {
            // Replacing or updating reuses the node's record: the previous inverter stops existing and no history is kept.
            currentInverter.SerieNumber = serie;
            currentInverter.NormalizedSerieNumber = normalizedSerie;
            currentInverter.BrandAndModel = brandAndModel;
            currentInverter.NominalCapacityKw = request.NominalCapacityKw;
            currentInverter.Endpoint = endpoint;
            _inverterRepository.Update(currentInverter);
        }
        else
        {
            _inverterRepository.Add(new Inverter
            {
                Id = Guid.NewGuid(),
                SerieNumber = serie,
                NormalizedSerieNumber = normalizedSerie,
                BrandAndModel = brandAndModel,
                NominalCapacityKw = request.NominalCapacityKw,
                Endpoint = endpoint,
                SolarNodeId = node.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        node.UpdatedAtUtc = DateTime.UtcNow;
        _repository.Update(node);
        _logger.LogInformation("Inverter {Serie} assigned to solar node {NodeId}", serie, node.Id);

        return ServiceResult.Ok();
    }

    public ServiceResult UnassignInverter(Guid nodeId)
    {
        var node = _repository.GetById(nodeId);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var inverter = _inverterRepository.GetByNodeId(node.Id);
        if (inverter == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo no tiene un inversor asignado.");
        }

        // Unassigning is allowed on inactive nodes so the inverter can always be freed.
        _inverterRepository.Remove(inverter);

        node.UpdatedAtUtc = DateTime.UtcNow;
        _repository.Update(node);
        _logger.LogInformation("Inverter {Serie} unassigned from solar node {NodeId}", inverter.SerieNumber, node.Id);

        return ServiceResult.Ok();
    }
}