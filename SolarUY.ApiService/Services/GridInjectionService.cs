using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Reglas de negocio del control de inyección a la red (spec 033): confirmación tipada "CONFIRMAR",
/// estados repetidos, parque inactivo, órdenes todo o nada y bitácora inmutable.
/// </summary>
public class GridInjectionService : IGridInjectionService
{
    public const string ConfirmationText = "CONFIRMAR";

    private readonly ISolarParkRepository _parkRepository;
    private readonly ISolarNodeRepository _nodeRepository;
    private readonly IInverterRepository _inverterRepository;
    private readonly IGridInjectionEventRepository _eventRepository;
    private readonly IInverterCommandSender _commandSender;
    private readonly ILogger<GridInjectionService> _logger;

    public GridInjectionService(
        ISolarParkRepository parkRepository,
        ISolarNodeRepository nodeRepository,
        IInverterRepository inverterRepository,
        IGridInjectionEventRepository eventRepository,
        IInverterCommandSender commandSender,
        ILogger<GridInjectionService> logger)
    {
        _parkRepository = parkRepository;
        _nodeRepository = nodeRepository;
        _inverterRepository = inverterRepository;
        _eventRepository = eventRepository;
        _commandSender = commandSender;
        _logger = logger;
    }

    public ServiceResult CutParkInjection(Guid parkId, string confirmationText, string orderedBy, string? remoteIp, string? reason)
    {
        var park = _parkRepository.GetById(parkId);
        if (park == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El parque solicitado no existe.");
        }

        // Caso límite defensivo: la UI no ofrece el botón sobre parques inactivos.
        if (park.Status == SolarParkStatus.Inactive)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "El parque está inactivo y no admite órdenes de inyección.");
        }

        // RF-3: confirmación tipada exacta (sin Trim ni mayúsculas forzadas).
        var confirmationError = ValidateConfirmation(confirmationText);
        if (confirmationError != null)
        {
            return confirmationError;
        }

        // Caso límite: corte repetido no reenvía órdenes.
        if (!park.GridInjectionEnabled)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "La inyección a la red ya se encuentra detenida.");
        }

        var inverters = GetParkInverters(parkId);

        // Orden "todo o nada": cualquier inversor que no confirme deja la operación sin efecto.
        var sendError = SendOrders(inverters, _commandSender.SendInjectionCut);
        if (sendError != null)
        {
            return sendError;
        }

        park.GridInjectionEnabled = false;
        _parkRepository.Update(park);

        foreach (var inverter in inverters)
        {
            inverter.GridInjectionEnabled = false;
            _inverterRepository.Update(inverter);
        }

        RegisterEvent(InjectionTarget.Park, park.Id, park.Name, park.Id, InjectionAction.Cut,
            orderedBy, remoteIp, reason, inverters.Count);
        _logger.LogInformation(
            "Inyección a la red detenida administrativamente para el parque {ParkId} ({OrdersSent} órdenes confirmadas)",
            park.Id, inverters.Count);

        return ServiceResult.Ok();
    }

    public ServiceResult ResumeParkInjection(Guid parkId, string confirmationText, string orderedBy, string? remoteIp, string? reason)
    {
        var park = _parkRepository.GetById(parkId);
        if (park == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El parque solicitado no existe.");
        }

        if (park.Status == SolarParkStatus.Inactive)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "El parque está inactivo y no admite órdenes de inyección.");
        }

        var confirmationError = ValidateConfirmation(confirmationText);
        if (confirmationError != null)
        {
            return confirmationError;
        }

        // Caso límite: reanudación repetida no reenvía órdenes.
        if (park.GridInjectionEnabled)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "La inyección a la red ya se encuentra activa.");
        }

        var inverters = GetParkInverters(parkId);

        var sendError = SendOrders(inverters, _commandSender.SendInjectionResume);
        if (sendError != null)
        {
            return sendError;
        }

        park.GridInjectionEnabled = true;
        _parkRepository.Update(park);

        foreach (var inverter in inverters)
        {
            inverter.GridInjectionEnabled = true;
            _inverterRepository.Update(inverter);
        }

        RegisterEvent(InjectionTarget.Park, park.Id, park.Name, park.Id, InjectionAction.Resume,
            orderedBy, remoteIp, reason, inverters.Count);
        _logger.LogInformation(
            "Inyección a la red reanudada administrativamente para el parque {ParkId} ({OrdersSent} órdenes confirmadas)",
            park.Id, inverters.Count);

        return ServiceResult.Ok();
    }

    public ServiceResult CutInverterInjection(Guid nodeId, string confirmationText, string orderedBy, string? remoteIp, string? reason)
    {
        var node = _nodeRepository.GetById(nodeId);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var confirmationError = ValidateConfirmation(confirmationText);
        if (confirmationError != null)
        {
            return confirmationError;
        }

        var inverter = _inverterRepository.GetByNodeId(node.Id);
        if (inverter == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo no tiene un inversor asignado.");
        }

        if (!inverter.GridInjectionEnabled)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "La inyección a la red ya se encuentra detenida.");
        }

        var sendError = SendOrders([inverter], _commandSender.SendInjectionCut);
        if (sendError != null)
        {
            return sendError;
        }

        inverter.GridInjectionEnabled = false;
        _inverterRepository.Update(inverter);

        RegisterEvent(InjectionTarget.Inverter, inverter.Id, inverter.SerieNumber, node.SolarParkId,
            InjectionAction.Cut, orderedBy, remoteIp, reason, 1);
        _logger.LogInformation(
            "Inyección a la red detenida administrativamente para el inversor {InverterSerie} del nodo {NodeId}",
            inverter.SerieNumber, node.Id);

        return ServiceResult.Ok();
    }

    public ServiceResult ResumeInverterInjection(Guid nodeId, string confirmationText, string orderedBy, string? remoteIp, string? reason)
    {
        var node = _nodeRepository.GetById(nodeId);
        if (node == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo solicitado no existe.");
        }

        var confirmationError = ValidateConfirmation(confirmationText);
        if (confirmationError != null)
        {
            return confirmationError;
        }

        var inverter = _inverterRepository.GetByNodeId(node.Id);
        if (inverter == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El nodo no tiene un inversor asignado.");
        }

        if (inverter.GridInjectionEnabled)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "La inyección a la red ya se encuentra activa.");
        }

        var sendError = SendOrders([inverter], _commandSender.SendInjectionResume);
        if (sendError != null)
        {
            return sendError;
        }

        inverter.GridInjectionEnabled = true;
        _inverterRepository.Update(inverter);

        RegisterEvent(InjectionTarget.Inverter, inverter.Id, inverter.SerieNumber, node.SolarParkId,
            InjectionAction.Resume, orderedBy, remoteIp, reason, 1);
        _logger.LogInformation(
            "Inyección a la red reanudada administrativamente para el inversor {InverterSerie} del nodo {NodeId}",
            inverter.SerieNumber, node.Id);

        return ServiceResult.Ok();
    }

    // RF-3: el texto debe coincidir exactamente con "CONFIRMAR".
    private static ServiceResult? ValidateConfirmation(string? confirmationText)
    {
        if (confirmationText != ConfirmationText)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "La confirmación no coincide. Escribí exactamente 'CONFIRMAR' para proceder.");
        }

        return null;
    }

    // Inversores del parque: uno por nodo (spec 013).
    private IReadOnlyList<Inverter> GetParkInverters(Guid parkId)
    {
        var nodeIds = _nodeRepository.GetByParkId(parkId).Select(n => n.Id).ToList();
        return _inverterRepository.GetByNodeIds(nodeIds);
    }

    // Envía la orden a cada inversor y aborta ante la primera pérdida de comunicación.
    private static ServiceResult? SendOrders(IReadOnlyList<Inverter> inverters, Func<string, string?, bool> send)
    {
        foreach (var inverter in inverters)
        {
            if (!send(inverter.SerieNumber, inverter.Endpoint))
            {
                return ServiceResult.Fail(
                    ServiceErrorType.Unavailable,
                    $"Error de comunicación con el inversor {inverter.SerieNumber}. La orden no pudo confirmarse.");
            }
        }

        return null;
    }

    // Bitácora inmutable: solo altas, con snapshot del objetivo, usuario, UTC, IP, motivo y órdenes confirmadas.
    private void RegisterEvent(InjectionTarget targetType, Guid targetId, string targetName, Guid parkId,
        InjectionAction action, string orderedBy, string? remoteIp, string? reason, int ordersSent)
    {
        var trimmedReason = reason?.Trim();
        _eventRepository.Add(new GridInjectionEvent
        {
            TargetType = targetType,
            TargetId = targetId,
            TargetName = targetName,
            ParkId = parkId,
            Action = action,
            OrderedBy = orderedBy,
            OrderedAtUtc = DateTime.UtcNow,
            RemoteIp = remoteIp,
            Reason = string.IsNullOrEmpty(trimmedReason) ? null : trimmedReason,
            OrdersSent = ordersSent
        });
    }
}
