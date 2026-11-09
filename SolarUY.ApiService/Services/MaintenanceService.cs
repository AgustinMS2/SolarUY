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

    public MaintenanceService(
        IMaintenanceRepository maintenanceRepository,
        ISolarPanelRepository panelRepository,
        ITechnicianRepository technicianRepository,
        ILogger<MaintenanceService> logger)
    {
        _maintenanceRepository = maintenanceRepository;
        _panelRepository = panelRepository;
        _technicianRepository = technicianRepository;
        _logger = logger;
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

        // RF-2: panel existente y activo.
        var panel = _panelRepository.GetById(request.SolarPanelId);
        if (panel == null)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.NotFound, "El panel solar solicitado no existe.");
        }

        if (panel.Status != SolarPanelStatus.Active)
        {
            return ServiceResult<Guid>.Fail(ServiceErrorType.Validation, "Debés seleccionar un panel solar activo.");
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

        return new MaintenanceOrderDto
        {
            Id = order.Id,
            SolarPanelId = order.SolarPanelId,
            SolarPanelSerie = panel?.SerieNumber,
            SolarParkName = parkName,
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
}
