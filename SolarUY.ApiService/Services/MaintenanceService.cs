using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly ISolarPanelRepository _panelRepository;
    private readonly ITechnicianRepository _technicianRepository;
    private readonly ILogger<MaintenanceService> _logger;
    private readonly IAlertService? _alertService;
    private readonly ISimulatedFaultRepository? _simulatedFaultRepository;

    public MaintenanceService(
        IMaintenanceRepository maintenanceRepository,
        ISolarPanelRepository panelRepository,
        ITechnicianRepository technicianRepository,
        ILogger<MaintenanceService> logger,
        IAlertService? alertService = null,
        ISimulatedFaultRepository? simulatedFaultRepository = null)
    {
        _maintenanceRepository = maintenanceRepository;
        _panelRepository = panelRepository;
        _technicianRepository = technicianRepository;
        _logger = logger;
        _alertService = alertService;
        _simulatedFaultRepository = simulatedFaultRepository;
    }

    public ServiceResult<Guid> CreateReservation(CreateMaintenanceReservationRequest request)
    {
        // RF-2: fecha futura, al menos 1 hora posterior al momento actual.
        if (request.ScheduledDateUtc <= DateTime.UtcNow.AddHours(1))
        {
            return ServiceResult<Guid>.Fail(
                ServiceErrorType.Validation,
                "La fecha y hora del mantenimiento debe ser posterior al momento actual.");
        }

        // RF-2 / Spec 039 RF-3: panel existente y no dado de baja (se permite Activo o Desactivado remotamente).
        var panel = _panelRepository.GetById(request.SolarPanelId);
        if (panel == null)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.NotFound, "El panel solar solicitado no existe.");
        }

        if (panel.Status == SolarPanelStatus.Inactive)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "Debés seleccionar un panel solar activo.");
        }

        // Spec 039 RF-4: el parque solar del panel debe estar activo.
        var park = panel.SolarPark ?? _panelRepository.GetParkById(panel.SolarParkId);
        if (park is { Status: SolarParkStatus.Inactive })
        {
            return ServiceResult<Guid>.Fail(
                ServiceErrorType.Validation,
                "El parque solar del panel seleccionado se encuentra inactivo.");
        }

        // RF-2: técnico existente y activo.
        var technician = _technicianRepository.GetById(request.TechnicianId);
        if (technician == null)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.NotFound, "El técnico solicitado no existe.");
        }

        if (technician.Status != TechnicianStatus.Active)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "Debés seleccionar un técnico activo.");
        }

        // RF-2: tipo de intervención válido.
        if (!Enum.IsDefined(request.Type))
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "Debés seleccionar el tipo de intervención.");
        }

        // RF-2: descripción entre 5 y 500 caracteres.
        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length < 5)
        {
            return ServiceResult<Guid>.Fail(
                ServiceErrorType.Validation,
                "La descripción del trabajo debe contener al menos 5 caracteres.");
        }

        if (description.Length > 500)
        {
            return ServiceResult<Guid>.Fail(
                ServiceErrorType.Validation,
                "La descripción del trabajo no puede superar los 500 caracteres.");
        }

        // RF-3: ya existe un mantenimiento pendiente para el panel ese día -> pedir confirmación.
        if (!request.ConfirmSecondIntervention &&
            _maintenanceRepository.HasPendingForPanel(request.SolarPanelId, request.ScheduledDateUtc))
        {
            return ServiceResult<Guid>.Fail(
                ServiceErrorType.ConfirmationRequired,
                "El panel ya tiene un mantenimiento asignado para esa fecha. ¿Deseás agendar una segunda intervención?");
        }

        // RF-1: orden en estado Pendiente, fecha persistida en UTC.
        var order = new MaintenanceOrder
        {
            Id = Guid.NewGuid(),
            SolarPanelId = request.SolarPanelId,
            TechnicianId = request.TechnicianId,
            Type = request.Type,
            ScheduledDateUtc = request.ScheduledDateUtc,
            Description = description,
            Status = MaintenanceStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow
        };

        _maintenanceRepository.Add(order);
        _logger.LogInformation("Maintenance order {OrderId} created for panel {PanelId}", order.Id, request.SolarPanelId);

        return ServiceResult<Guid>.Ok(order.Id);
    }

    public ServiceResult<MaintenanceOrderDto> GetById(Guid orderId)
    {
        var order = _maintenanceRepository.GetById(orderId);
        if (order == null)
        {
            return ServiceResult<MaintenanceOrderDto>.Fail(ServiceErrorType.NotFound, "El mantenimiento solicitado no existe.");
        }

        return ServiceResult<MaintenanceOrderDto>.Ok(MapToDto(order));
    }

    public ServiceResult<IReadOnlyList<MaintenanceOrderDto>> GetAll(MaintenanceFilter filter)
    {
        // RF-2/RF-3: ordenamiento cronológico descendente y filtros combinables (los aplica el repositorio).
        var orders = _maintenanceRepository.GetAll(filter);
        var dtoList = orders.Select(MapToDto).ToList();

        return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Ok(dtoList.AsReadOnly());
    }

    private MaintenanceOrderDto MapToDto(MaintenanceOrder order)
    {
        // Con repositorio EF vienen precargadas las navegaciones; con fakes de tests se resuelven vía repos.
        var panel = order.SolarPanel ?? _panelRepository.GetById(order.SolarPanelId);
        var parkName = panel?.SolarPark?.Name
            ?? (panel != null ? _panelRepository.GetParkById(panel.SolarParkId)?.Name : null);
        var technicianName = order.Technician?.FullName
            ?? _technicianRepository.GetById(order.TechnicianId)?.FullName;
        var nodeName = panel?.SolarNode?.Name;

        return new MaintenanceOrderDto
        {
            Id = order.Id,
            SolarPanelId = order.SolarPanelId,
            SolarPanelSerie = panel?.SerieNumber,
            SolarParkName = parkName,
            SolarNodeName = nodeName,
            TechnicianId = order.TechnicianId,
            TechnicianName = technicianName,
            Type = order.Type,
            ScheduledDateUtc = order.ScheduledDateUtc,
            Description = order.Description,
            Status = order.Status
        };
    }

    public ServiceResult UpdateReservation(Guid orderId, UpdateMaintenanceReservationRequest request)
    {
        var order = _maintenanceRepository.GetById(orderId);
        if (order == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El mantenimiento solicitado no existe.");
        }

        // RF-3: solo se modifican órdenes Pendientes.
        if (order.Status != MaintenanceStatus.Pending)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Conflict,
                "No se pueden modificar mantenimientos que ya han sido completados o cancelados.");
        }

        // RF-4: la nueva fecha debe ser posterior al momento actual.
        if (request.ScheduledDateUtc <= DateTime.UtcNow)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Validation,
                "La nueva fecha programada debe ser posterior al momento actual.");
        }

        // El nuevo técnico debe existir y estar activo.
        var technician = _technicianRepository.GetById(request.TechnicianId);
        if (technician == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El técnico solicitado no existe.");
        }

        if (technician.Status != TechnicianStatus.Active)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "Debés seleccionar un técnico activo.");
        }

        // La descripción debe tener entre 5 y 500 caracteres.
        var description = request.Description?.Trim() ?? string.Empty;
        if (description.Length < 5)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Validation,
                "La descripción del trabajo debe contener al menos 5 caracteres.");
        }

        if (description.Length > 500)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Validation,
                "La descripción del trabajo no puede superar los 500 caracteres.");
        }

        // Caso límite cubierto por spec: los tres campos son modificables de forma independiente.
        order.TechnicianId = request.TechnicianId;
        order.ScheduledDateUtc = request.ScheduledDateUtc;
        order.Description = description;

        // RF-2: registrar la fecha de modificación en UTC.
        order.UpdatedAtUtc = DateTime.UtcNow;
        _maintenanceRepository.Update(order);
        _logger.LogInformation("Maintenance order {OrderId} updated", orderId);

        return ServiceResult.Ok();
    }

    public ServiceResult CancelReservation(Guid orderId, CancelMaintenanceRequest request)
    {
        var order = _maintenanceRepository.GetById(orderId);
        if (order == null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "El mantenimiento solicitado no existe.");
        }

        // RF-4: solo las órdenes Pendientes pueden cancelarse.
        if (order.Status != MaintenanceStatus.Pending)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "El mantenimiento ya se encuentra finalizado o cancelado.");
        }

        // RF-3: motivo obligatorio, al menos 10 caracteres.
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length < 10)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Validation,
                "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres.");
        }

        if (reason.Length > 500)
        {
            return ServiceResult.Fail(
                ServiceErrorType.Validation,
                "El motivo de cancelación no puede superar los 500 caracteres.");
        }

        // RF-2: estado Cancelado, motivo y fecha registrados en UTC.
        order.Status = MaintenanceStatus.Cancelled;
        order.CancellationReason = reason;
        order.UpdatedAtUtc = DateTime.UtcNow;
        _maintenanceRepository.Update(order);
        _logger.LogInformation("Maintenance order {OrderId} cancelled", orderId);

        return ServiceResult.Ok();
    }

    // Spec 026: el técnico (o el administrador, por contingencia) confirma una intervención realizada.
    public ServiceResult<MaintenanceConfirmation> ConfirmMaintenance(
        Guid orderId,
        ConfirmMaintenanceRequest request,
        string? callerEmail = null,
        bool isAdmin = true)
    {
        var order = _maintenanceRepository.GetById(orderId);
        if (order == null)
        {
            return ServiceResult<MaintenanceConfirmation>.Fail(ServiceErrorType.NotFound, "El mantenimiento solicitado no existe.");
        }

        // RF-4: solo las órdenes Pendientes pueden confirmarse (ni las Completadas ni las Canceladas).
        if (order.Status != MaintenanceStatus.Pending)
        {
            return ServiceResult<MaintenanceConfirmation>.Fail(ServiceErrorType.Conflict, "Este mantenimiento ya fue completado previamente.");
        }

        // Spec 039 RF-2: si quien confirma no es Administrador, la orden debe estar asignada a su técnico.
        if (!isAdmin)
        {
            var callerTech = !string.IsNullOrWhiteSpace(callerEmail)
                ? _technicianRepository.GetByEmail(callerEmail)
                : null;
            if (callerTech == null || callerTech.Id != order.TechnicianId)
            {
                return ServiceResult<MaintenanceConfirmation>.Fail(
                    ServiceErrorType.Conflict,
                    "No tenés permiso para confirmar un mantenimiento asignado a otro técnico.");
            }
        }

        // RF-2: observaciones obligatorias, de 10 a 1000 caracteres.
        var notes = request.Notes?.Trim() ?? string.Empty;
        if (notes.Length < 10)
        {
            return ServiceResult<MaintenanceConfirmation>.Fail(
                ServiceErrorType.Validation,
                "Las observaciones técnicas son obligatorias y deben tener al menos 10 caracteres.");
        }

        if (notes.Length > 1000)
        {
            return ServiceResult<MaintenanceConfirmation>.Fail(
                ServiceErrorType.Validation,
                "Las observaciones técnicas no pueden superar los 1000 caracteres.");
        }

        // Componentes reemplazados: opcionales, hasta 300 caracteres; vacío = sin valor.
        var replacedParts = string.IsNullOrWhiteSpace(request.ReplacedParts) ? null : request.ReplacedParts.Trim();
        if (replacedParts != null && replacedParts.Length > 300)
        {
            return ServiceResult<MaintenanceConfirmation>.Fail(
                ServiceErrorType.Validation,
                "Los componentes reemplazados no pueden superar los 300 caracteres.");
        }

        // Estado del panel tras la tarea: coincidencia exacta (no se aceptan números ni otros textos).
        MaintenancePanelOutcome? outcome = request.PanelOutcome switch
        {
            "Operational" => MaintenancePanelOutcome.Operational,
            "NeedsFollowUp" => MaintenancePanelOutcome.NeedsFollowUp,
            _ => null
        };
        if (outcome == null)
        {
            return ServiceResult<MaintenanceConfirmation>.Fail(ServiceErrorType.Validation, "Seleccioná el estado del panel tras la tarea.");
        }

        // RF-1: la orden queda Completada con la fecha real de finalización en UTC.
        var now = DateTime.UtcNow;
        order.Status = MaintenanceStatus.Completed;
        order.ResolutionNotes = notes;
        order.ReplacedParts = replacedParts;
        order.PanelOutcome = outcome;
        order.CompletedAtUtc = now;
        order.UpdatedAtUtc = now;
        _maintenanceRepository.Update(order);
        _logger.LogInformation("Maintenance order {OrderId} confirmed as completed", orderId);

        // Spec 039 RF-1: si el panel quedó Operativo, finaliza las fallas simuladas activas del panel.
        if (outcome == MaintenancePanelOutcome.Operational)
        {
            _simulatedFaultRepository?.EndByTarget(SimulatedFaultTarget.Panel, order.SolarPanelId, now);
        }

        // RF-3: las alertas activas del panel quedan como Atendidas (spec 031). Solo si la confirmación fue aceptada.
        var attendedAlerts = _alertService?.AttendPanelAlerts(order.SolarPanelId) ?? [];

        return ServiceResult<MaintenanceConfirmation>.Ok(new MaintenanceConfirmation(attendedAlerts));
    }

    public ServiceResult<IReadOnlyList<MaintenanceOrderDto>> GetMyMaintenances(string technicianEmail)
    {
        if (string.IsNullOrWhiteSpace(technicianEmail))
        {
            return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Fail(
                ServiceErrorType.Validation,
                "El correo electrónico es obligatorio.");
        }

        var technician = _technicianRepository.GetByEmail(technicianEmail);
        if (technician == null)
        {
            return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Fail(
                ServiceErrorType.NotFound,
                "No se encontró un perfil de técnico asociado a tu usuario.");
        }

        var orders = _maintenanceRepository.GetByTechnicianId(technician.Id);

        // RF-2: priorizando las órdenes "Pendientes" por fecha programada ascendente,
        // luego las completadas/canceladas por fecha descendente.
        var pending = orders
            .Where(o => o.Status == MaintenanceStatus.Pending)
            .OrderBy(o => o.ScheduledDateUtc);

        var others = orders
            .Where(o => o.Status != MaintenanceStatus.Pending)
            .OrderByDescending(o => o.ScheduledDateUtc);

        var sortedOrders = pending.Concat(others).ToList();
        var dtos = sortedOrders.Select(MapToDto).ToList();

        return ServiceResult<IReadOnlyList<MaintenanceOrderDto>>.Ok(dtos.AsReadOnly());
    }
}
