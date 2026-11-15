using Microsoft.Extensions.Options;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

public class SimulationFaultService : ISimulationFaultService
{
    private const string DisabledMessage = "El simulador de fallas no está habilitado en este entorno.";

    private readonly ISimulatedFaultRepository _faultRepository;
    private readonly ISolarPanelRepository _panelRepository;
    private readonly ISolarNodeRepository _nodeRepository;
    private readonly IInverterRepository _inverterRepository;
    private readonly ISolarParkRepository _parkRepository;
    private readonly IOptions<SimulationOptions> _options;
    private readonly TimeProvider _timeProvider;

    public SimulationFaultService(
        ISimulatedFaultRepository faultRepository,
        ISolarPanelRepository panelRepository,
        ISolarNodeRepository nodeRepository,
        IInverterRepository inverterRepository,
        ISolarParkRepository parkRepository,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider)
    {
        _faultRepository = faultRepository;
        _panelRepository = panelRepository;
        _nodeRepository = nodeRepository;
        _inverterRepository = inverterRepository;
        _parkRepository = parkRepository;
        _options = options;
        _timeProvider = timeProvider;
    }

    public ServiceResult<string> Inject(SimulatedFaultTarget targetType, Guid targetId, SimulatedFaultType faultType, string startedBy)
    {
        if (!_options.Value.FaultInjectionEnabled)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Unavailable, DisabledMessage);
        }

        if (!FaultCatalog.TypesFor(targetType).Contains(faultType))
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Validation, "El tipo de falla no corresponde al equipo seleccionado.");
        }

        var equipment = Find(targetType, targetId);
        if (equipment is null)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.NotFound, $"{Capitalized(targetType)} solicitado no existe.");
        }

        // Solo se inyecta sobre equipos que emiten lecturas (spec 037, casos límite).
        var notEmitting = NotEmittingReason(targetType, equipment);
        if (notEmitting is not null)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Conflict, notEmitting);
        }

        if (_faultRepository.GetActiveByTarget(targetType, targetId).Any(f => f.FaultType == faultType))
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Conflict, "El equipo ya tiene esa falla activa.");
        }

        _faultRepository.Add(new SimulatedFault
        {
            Id = Guid.NewGuid(),
            FaultType = faultType,
            TargetType = targetType,
            TargetId = targetId,
            SolarParkId = equipment.ParkId,
            StartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime,
            StartedBy = startedBy
        });

        return ServiceResult<string>.Ok($"Falla simulada inyectada con éxito en {Noun(targetType)} \"{equipment.Serie}\".");
    }

    public ServiceResult<string> ClearTarget(SimulatedFaultTarget targetType, Guid targetId)
    {
        if (!_options.Value.FaultInjectionEnabled)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Unavailable, DisabledMessage);
        }

        // Un equipo dado de baja con una falla activa también se puede restablecer: solo debe existir.
        var equipment = Find(targetType, targetId);
        if (equipment is null)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.NotFound, $"{Capitalized(targetType)} solicitado no existe.");
        }

        var ended = _faultRepository.EndByTarget(targetType, targetId, _timeProvider.GetUtcNow().UtcDateTime);

        return ServiceResult<string>.Ok(ended > 0
            ? $"Falla simulada d{Noun(targetType)} \"{equipment.Serie}\" restablecida."
            : $"{Capitalized(targetType)} \"{equipment.Serie}\" no tenía fallas simuladas activas.");
    }

    public ServiceResult<string> ClearAll()
    {
        if (!_options.Value.FaultInjectionEnabled)
        {
            return ServiceResult<string>.Fail(ServiceErrorType.Unavailable, DisabledMessage);
        }

        var ended = _faultRepository.EndAll(_timeProvider.GetUtcNow().UtcDateTime);

        return ServiceResult<string>.Ok(ended > 0
            ? "Todas las fallas simuladas fueron restablecidas."
            : "No había fallas simuladas activas.");
    }

    public SimulationFaultOverviewDto GetOverview()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var timeline = FaultTimeline.From(_faultRepository.GetActive());
        var parkNames = _parkRepository.GetAll().ToDictionary(p => p.Id, p => p.Name);
        var activeNodes = _nodeRepository.GetAll().Where(n => n.Status == SolarNodeStatus.Active).ToDictionary(n => n.Id);

        SimulationFaultTargetDto ToDto(Guid id, string serie, Guid parkId, string nodeName)
        {
            var fault = timeline.EffectiveFault(id, now);
            return new SimulationFaultTargetDto(
                id,
                serie,
                parkNames.GetValueOrDefault(parkId, string.Empty),
                nodeName,
                fault,
                fault.HasValue ? FaultCatalog.LabelOf(fault.Value) : null,
                fault.HasValue ? FaultCatalog.SeverityOf(fault.Value) : null);
        }

        var panels = _panelRepository.GetAll()
            .Where(p => p.Status == SolarPanelStatus.Active && p.SolarNodeId is { } nodeId && activeNodes.ContainsKey(nodeId))
            .Select(p => ToDto(p.Id, p.SerieNumber, p.SolarParkId, activeNodes[p.SolarNodeId!.Value].Name))
            .OrderBy(d => d.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.SerieNumber, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var inverters = activeNodes.Count == 0
            ? new List<SimulationFaultTargetDto>()
            : _inverterRepository.GetByNodeIds(activeNodes.Keys.ToList())
                .Select(i => ToDto(i.Id, i.SerieNumber, activeNodes[i.SolarNodeId].SolarParkId, activeNodes[i.SolarNodeId].Name))
                .OrderBy(d => d.ParkName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.SerieNumber, StringComparer.OrdinalIgnoreCase)
                .ToList();

        return new SimulationFaultOverviewDto(_options.Value.FaultInjectionEnabled, panels, inverters);
    }

    /// <summary>Lo que hace falta saber de un panel o un inversor para inyectarle una falla.</summary>
    private sealed record Equipment(string Serie, Guid ParkId, bool IsActive, bool HasNode, SolarNodeStatus? NodeStatus);

    private Equipment? Find(SimulatedFaultTarget targetType, Guid targetId)
    {
        if (targetType == SimulatedFaultTarget.Panel)
        {
            var panel = _panelRepository.GetById(targetId);
            if (panel is null)
            {
                return null;
            }

            var node = panel.SolarNodeId is { } nodeId ? _nodeRepository.GetById(nodeId) : null;
            return new Equipment(panel.SerieNumber, panel.SolarParkId, panel.Status == SolarPanelStatus.Active, panel.SolarNodeId is not null, node?.Status);
        }

        var inverter = _inverterRepository.GetById(targetId);
        if (inverter is null)
        {
            return null;
        }

        var inverterNode = _nodeRepository.GetById(inverter.SolarNodeId);
        return new Equipment(inverter.SerieNumber, inverterNode?.SolarParkId ?? Guid.Empty, true, true, inverterNode?.Status);
    }

    private static string? NotEmittingReason(SimulatedFaultTarget targetType, Equipment equipment)
    {
        if (targetType == SimulatedFaultTarget.Panel)
        {
            if (!equipment.IsActive)
            {
                return "El panel está dado de baja y no emite lecturas.";
            }

            if (!equipment.HasNode)
            {
                return "El panel no está asignado a un nodo y no emite lecturas.";
            }
        }

        return equipment.NodeStatus == SolarNodeStatus.Active
            ? null
            : $"El nodo d{Noun(targetType)} está inactivo y no emite lecturas.";
    }

    // "el panel" / "el inversor": se usa solo ("en el panel") o con "d" delante ("del panel").
    private static string Noun(SimulatedFaultTarget targetType)
        => targetType == SimulatedFaultTarget.Panel ? "el panel" : "el inversor";

    private static string Capitalized(SimulatedFaultTarget targetType)
        => targetType == SimulatedFaultTarget.Panel ? "El panel" : "El inversor";
}
