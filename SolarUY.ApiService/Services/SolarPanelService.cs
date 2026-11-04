using System.Globalization;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class SolarPanelService : ISolarPanelService
{
    private const int MaxPanelsPerOperation = 50;
    private const string InactiveAssignmentMessage = "No se pueden asociar paneles inactivos o a nodos inactivos.";
    private const string MissingPanelsMessage = "Uno o más paneles seleccionados no existen.";

    private readonly ISolarPanelRepository _repository;
    private readonly ISolarNodeRepository _nodeRepository;
    private readonly IInverterRepository _inverterRepository;
    private readonly ILogger<SolarPanelService> _logger;

    public SolarPanelService(
        ISolarPanelRepository repository,
        ISolarNodeRepository nodeRepository,
        IInverterRepository inverterRepository,
        ILogger<SolarPanelService> logger)
    {
        _repository = repository;
        _nodeRepository = nodeRepository;
        _inverterRepository = inverterRepository;
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
                p.SolarParkId,
                p.SolarNode?.Name,
                p.SolarNodeId
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
            panel.SolarParkId,
            panel.SolarNode?.Name,
            panel.SolarNodeId
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

    public ServiceResult DeletePanel(Guid id)
    {
        // 1. Validar que el panel existe
        var panel = _repository.GetById(id);
        if (panel == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El panel solicitado no existe.");
        }

        // 2. Validar que el panel no está ya inactivo
        if (panel.Status == SolarPanelStatus.Inactive)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "El panel ya está inactivo.");
        }

        // 3. Cambiar estado a Inactivo (baja lógica)
        panel.Status = SolarPanelStatus.Inactive;
        panel.UpdatedAtUtc = DateTime.UtcNow;

        _repository.Update(panel);
        _logger.LogInformation("Solar panel deleted successfully with ID {PanelId}", panel.Id);

        // 4. Retornar éxito
        return ServiceResult.Ok();
    }

    public ServiceResult AssignPanelsToNode(Guid nodeId, AssignPanelsRequest request)
    {
        var node = _nodeRepository.GetById(nodeId);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var batchError = ValidateBatch(request.PanelIds, out var panelIds);
        if (batchError != null)
        {
            return batchError;
        }

        if (node.Status == SolarNodeStatus.Inactive)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, InactiveAssignmentMessage);
        }

        var found = _repository.GetByIds(panelIds);
        if (found.Count != panelIds.Count)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, MissingPanelsMessage);
        }

        if (found.Any(p => p.Status == SolarPanelStatus.Inactive))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, InactiveAssignmentMessage);
        }

        if (found.Any(p => p.SolarParkId != node.SolarParkId))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "No se pueden asociar paneles de otro parque.");
        }

        // Keep the order chosen by the user so messages always mention the first panel they selected.
        var order = panelIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var toAssign = found
            .Where(p => p.SolarNodeId != node.Id)
            .OrderBy(p => order[p.Id])
            .ToList();

        // Panels that already belong to the node are ignored: nothing to change, nothing to warn about.
        if (toAssign.Count == 0)
        {
            return ServiceResult.Ok();
        }

        if (!request.Confirm)
        {
            var warnings = BuildAssignmentWarnings(node, toAssign);
            if (warnings.Count > 0)
            {
                return ServiceResult.Fail(ServiceErrorType.ConfirmationRequired, string.Join("\n", warnings));
            }
        }

        var now = DateTime.UtcNow;
        foreach (var panel in toAssign)
        {
            panel.SolarNodeId = node.Id;
            panel.UpdatedAtUtc = now;
        }

        _repository.UpdateRange(toAssign);
        _logger.LogInformation("{Count} solar panels assigned to solar node {NodeId}", toAssign.Count, node.Id);

        return ServiceResult.Ok();
    }

    private List<string> BuildAssignmentWarnings(SolarNode node, List<SolarPanel> toAssign)
    {
        var warnings = new List<string>();

        // RF-3: panels that currently belong to another node.
        var moved = toAssign.Where(p => p.SolarNodeId != null).ToList();
        if (moved.Count > 0)
        {
            var first = moved[0];
            var originName = _nodeRepository.GetById(first.SolarNodeId!.Value)?.Name ?? string.Empty;
            var message = $"El panel \"{first.SerieNumber}\" ya pertenece al nodo \"{originName}\". ¿Deseás reasignarlo a este nodo?";
            if (moved.Count > 1)
            {
                var others = moved.Count - 1;
                message += $" Se reasignarán también {others} {(others == 1 ? "panel más" : "paneles más")}.";
            }

            warnings.Add(message);
        }

        // RF-6: accumulated power of the node's active panels (after the assignment) against the inverter capacity.
        var inverter = _inverterRepository.GetByNodeId(node.Id);
        if (inverter != null)
        {
            var currentPowerW = _repository.GetByNodeId(node.Id)
                .Where(p => p.Status == SolarPanelStatus.Active)
                .Sum(p => (long)p.PowerW);
            var totalKw = (currentPowerW + toAssign.Sum(p => (long)p.PowerW)) / 1000.0;

            if (totalKw > inverter.NominalCapacityKw)
            {
                warnings.Add(
                    $"La potencia acumulada de los paneles ({FormatKw(totalKw)} kW) supera la capacidad nominal del inversor ({FormatKw(inverter.NominalCapacityKw)} kW).");
            }
        }

        return warnings;
    }

    private static string FormatKw(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Shared by assigning and unassigning: 1 to 50 distinct panels.
    private static ServiceResult? ValidateBatch(IReadOnlyList<Guid>? requestedIds, out List<Guid> panelIds)
    {
        panelIds = requestedIds?.Distinct().ToList() ?? [];

        if (panelIds.Count == 0)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "Seleccioná al menos un panel.");
        }

        if (panelIds.Count > MaxPanelsPerOperation)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "Se pueden asignar hasta 50 paneles a la vez.");
        }

        return null;
    }

    public ServiceResult UnassignPanelsFromNode(Guid nodeId, UnassignPanelsRequest request)
    {
        var node = _nodeRepository.GetById(nodeId);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var batchError = ValidateBatch(request.PanelIds, out var panelIds);
        if (batchError != null)
        {
            return batchError;
        }

        var found = _repository.GetByIds(panelIds);
        if (found.Count != panelIds.Count)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, MissingPanelsMessage);
        }

        // Unassigning is allowed on inactive nodes and for inactive panels so they can always be freed.
        var order = panelIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var notInNode = found.Where(p => p.SolarNodeId != node.Id).OrderBy(p => order[p.Id]).FirstOrDefault();
        if (notInNode != null)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Validation,
                $"El panel \"{notInNode.SerieNumber}\" no está asociado a este nodo.");
        }

        var now = DateTime.UtcNow;
        foreach (var panel in found)
        {
            panel.SolarNodeId = null;
            panel.UpdatedAtUtc = now;
        }

        _repository.UpdateRange(found);
        _logger.LogInformation("{Count} solar panels unassigned from solar node {NodeId}", found.Count, node.Id);

        return ServiceResult.Ok();
    }
}