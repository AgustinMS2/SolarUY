using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Órdenes remotas de activación/desactivación eléctrica de un panel (spec 032).
/// Reglas: roles y estados válidos, bloqueo por mantenimiento pendiente (RF-5),
/// orden al inversor con manejo de pérdida de comunicación y bitácora de auditoría.
/// </summary>
public class PanelControlService : IPanelControlService
{
    private readonly ISolarPanelRepository _panelRepository;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPanelControlEventRepository _controlEventRepository;
    private readonly IInverterRepository _inverterRepository;
    private readonly IInverterCommandSender _commandSender;
    private readonly ILogger<PanelControlService> _logger;

    public PanelControlService(
        ISolarPanelRepository panelRepository,
        IMaintenanceRepository maintenanceRepository,
        IPanelControlEventRepository controlEventRepository,
        IInverterRepository inverterRepository,
        IInverterCommandSender commandSender,
        ILogger<PanelControlService> logger)
    {
        _panelRepository = panelRepository;
        _maintenanceRepository = maintenanceRepository;
        _controlEventRepository = controlEventRepository;
        _inverterRepository = inverterRepository;
        _commandSender = commandSender;
        _logger = logger;
    }

    // RF-1/RF-2: corte eléctrico remoto de un panel activo.
    public ServiceResult Deactivate(Guid panelId, string orderedBy)
    {
        var panel = _panelRepository.GetById(panelId);
        if (panel == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El panel solar solicitado no existe.");
        }

        // Defensivo (la UI nunca ofrece el botón en estos estados).
        if (panel.Status == SolarPanelStatus.Deactivated)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "El panel ya se encuentra desactivado.");
        }

        if (panel.Status == SolarPanelStatus.Inactive)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Conflict,
                "El panel está dado de baja y no admite órdenes de control remoto.");
        }

        return SendOrder(panel, PanelControlAction.Deactivate, SolarPanelStatus.Deactivated, orderedBy);
    }

    // RF-3/RF-4: reactivación de un panel desactivado.
    public ServiceResult Activate(Guid panelId, string orderedBy)
    {
        var panel = _panelRepository.GetById(panelId);
        if (panel == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El panel solar solicitado no existe.");
        }

        if (panel.Status != SolarPanelStatus.Deactivated)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Conflict,
                "El panel no se encuentra desactivado.");
        }

        // RF-5: cualquier orden de mantenimiento Pendiente indica personal trabajando en el panel.
        if (_maintenanceRepository.HasPendingOrderForPanel(panelId))
        {
            return ServiceResult.Fail(
                ServiceErrorType.Conflict,
                "No se puede reactivar el panel mientras haya personal trabajando en un mantenimiento activo.");
        }

        return SendOrder(panel, PanelControlAction.Activate, SolarPanelStatus.Active, orderedBy);
    }

    // Envía la orden al inversor (si lo hay), cambia el estado y registra la bitácora.
    private ServiceResult SendOrder(
        SolarPanel panel, PanelControlAction action, SolarPanelStatus newStatus, string orderedBy)
    {
        // El panel no conoce el inversor directamente: Panel -> Nodo -> Inversor (spec 013).
        var inverter = panel.SolarNodeId.HasValue
            ? _inverterRepository.GetByNodeId(panel.SolarNodeId.Value)
            : null;

        var orderSent = false;
        if (inverter != null)
        {
            orderSent = action == PanelControlAction.Deactivate
                ? _commandSender.SendDeactivate(panel.SerieNumber, inverter.Endpoint)
                : _commandSender.SendActivate(panel.SerieNumber, inverter.Endpoint);

            if (!orderSent)
            {
                // Caso límite: se mantiene el estado previo y no se registra evento.
                _logger.LogWarning(
                    "Orden {Action} del panel {PanelId} no confirmada por el inversor {InverterSerie}",
                    action, panel.Id, inverter.SerieNumber);
                return ServiceResult.Fail(
                    ServiceErrorType.Unavailable,
                    "Error de comunicación con el inversor. La orden no pudo confirmarse.");
            }
        }

        var previousStatus = panel.Status;
        panel.Status = newStatus;
        panel.UpdatedAtUtc = DateTime.UtcNow;
        _panelRepository.Update(panel);

        _controlEventRepository.Add(new PanelControlEvent
        {
            Id = Guid.NewGuid(),
            SolarPanelId = panel.Id,
            Action = action,
            OrderedBy = orderedBy,
            OrderedAtUtc = DateTime.UtcNow,
            OrderSentToInverter = orderSent,
            InverterSerieNumber = inverter?.SerieNumber
        });

        _logger.LogInformation(
            "Panel {PanelId}: {Action} por {OrderedBy} (estado {Previous} -> {New}, orden enviada: {OrderSent})",
            panel.Id, action, orderedBy, previousStatus, newStatus, orderSent);

        return ServiceResult.Ok();
    }
}
