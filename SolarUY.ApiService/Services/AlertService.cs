using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Reglas de la Spec 031. Una "clave" de alerta es la combinación Severidad + Tipo de equipo + Equipo.
/// </summary>
public class AlertService : IAlertService
{
    // RF-2: ventana de deduplicado.
    private static readonly TimeSpan DuplicateWindow = TimeSpan.FromSeconds(60);

    // RF-6: ventana de consolidación y cantidad de alertas a partir de la cual se agrupan.
    private static readonly TimeSpan ConsolidationWindow = TimeSpan.FromMinutes(5);
    private const int ConsolidationThreshold = 3;

    // RF-5: antigüedad mínima de una alerta Descartada para purgarla y horario nocturno (hora de Uruguay).
    private static readonly TimeSpan PurgeAge = TimeSpan.FromHours(24);
    private const int NightStartHour = 0;
    private const int NightEndHour = 6;

    // Uruguay no tiene horario de verano; la spec 021 ya usa UTC-3 constante.
    private static readonly TimeSpan UruguayOffset = TimeSpan.FromHours(-3);

    private readonly IAlertRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AlertService(IAlertRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public AlertRegistration Register(FaultDetection fault)
    {
        ArgumentNullException.ThrowIfNull(fault);

        var now = Now();

        // RF-2: otra alerta de la misma clave, en cualquier estado, dentro de los últimos 60 s.
        var latest = _repository.GetLatest(fault.Severity, fault.EquipmentType, fault.EquipmentId);
        if (latest is not null && now - latest.LastOccurrenceAtUtc < DuplicateWindow)
        {
            return new AlertRegistration(AlertRegistrationOutcome.Discarded, null);
        }

        // RF-6: alertas abiertas (Pendientes o Reconocidas) de la clave en los últimos 5 minutos.
        var open = _repository.GetOpenSince(fault.Severity, fault.EquipmentType, fault.EquipmentId, now - ConsolidationWindow);

        var consolidated = open
            .Where(a => a.OccurrenceCount >= ConsolidationThreshold)
            .OrderByDescending(a => a.LastOccurrenceAtUtc)
            .FirstOrDefault();

        if (consolidated is not null)
        {
            consolidated.OccurrenceCount++;
            consolidated.LastOccurrenceAtUtc = now;
            _repository.Update(consolidated);
            return new AlertRegistration(AlertRegistrationOutcome.Consolidated, ToDto(consolidated));
        }

        // Esta detección sería la tercera (o más) dentro de la ventana: se fusionan en la alerta más antigua.
        if (open.Count >= ConsolidationThreshold - 1)
        {
            var survivor = open.OrderBy(a => a.CreatedAtUtc).First();
            var merged = open.Where(a => a.Id != survivor.Id).ToList();

            survivor.OccurrenceCount = open.Sum(a => a.OccurrenceCount) + 1;
            survivor.LastOccurrenceAtUtc = now;

            // Si alguna de las fusionadas seguía sin reconocer, la consolidada también (la campana debe seguir avisando).
            if (open.Any(a => a.Status == AlertStatus.Pending))
            {
                survivor.Status = AlertStatus.Pending;
                survivor.AcknowledgedAtUtc = null;
            }

            _repository.RemoveRange(merged);
            _repository.Update(survivor);
            return new AlertRegistration(AlertRegistrationOutcome.Consolidated, ToDto(survivor));
        }

        var alert = new Alert
        {
            Id = Guid.NewGuid(),
            SolarParkId = fault.ParkId,
            ParkName = fault.ParkName,
            EquipmentType = fault.EquipmentType,
            EquipmentId = fault.EquipmentId,
            EquipmentName = fault.EquipmentName,
            Severity = fault.Severity,
            Status = AlertStatus.Pending,
            Description = fault.Description,
            OccurrenceCount = 1,
            CreatedAtUtc = now,
            LastOccurrenceAtUtc = now
        };

        _repository.Add(alert);
        return new AlertRegistration(AlertRegistrationOutcome.Created, ToDto(alert));
    }

    public ServiceResult<AlertDto> Acknowledge(Guid id)
    {
        var alert = _repository.GetById(id);
        if (alert is null)
        {
            return NotFound();
        }

        // Idempotente: solo una alerta Pendiente cambia; si otro usuario ya la reconoció (o está más avanzada) no pasa nada.
        if (alert.Status == AlertStatus.Pending)
        {
            alert.Status = AlertStatus.Acknowledged;
            alert.AcknowledgedAtUtc = Now();
            _repository.Update(alert);
        }

        return ServiceResult<AlertDto>.Ok(ToDto(alert));
    }

    public ServiceResult<AlertDto> Dismiss(Guid id)
    {
        var alert = _repository.GetById(id);
        if (alert is null)
        {
            return NotFound();
        }

        if (alert.Status != AlertStatus.Acknowledged)
        {
            return ServiceResult<AlertDto>.Fail(ServiceErrorType.Conflict, "Solo se pueden descartar alertas reconocidas.");
        }

        alert.Status = AlertStatus.Dismissed;
        alert.DismissedAtUtc = Now();
        _repository.Update(alert);

        return ServiceResult<AlertDto>.Ok(ToDto(alert));
    }

    public AlertListDto GetRecent(int take)
    {
        var alerts = _repository.GetRecent(take).Select(ToDto).ToList();
        return new AlertListDto(GetUnreadCount(), alerts);
    }

    public int GetUnreadCount()
    {
        return _repository.CountByStatus(AlertStatus.Pending);
    }

    public int PurgeDismissed()
    {
        var cutoff = Now() - PurgeAge;

        var toRemove = _repository.GetDismissedCreatedBefore(cutoff)
            .Where(a => IsNocturnal(a.CreatedAtUtc))
            .ToList();

        _repository.RemoveRange(toRemove);
        return toRemove.Count;
    }

    public static AlertDto ToDto(Alert alert)
    {
        ArgumentNullException.ThrowIfNull(alert);

        return new AlertDto(
            alert.Id,
            alert.SolarParkId,
            alert.ParkName,
            alert.EquipmentType,
            alert.EquipmentId,
            alert.EquipmentName,
            alert.Severity,
            alert.Status,
            alert.Description,
            alert.OccurrenceCount,
            alert.CreatedAtUtc,
            alert.LastOccurrenceAtUtc,
            alert.AcknowledgedAtUtc);
    }

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;

    private static bool IsNocturnal(DateTime createdAtUtc)
    {
        var localHour = (createdAtUtc + UruguayOffset).Hour;
        return localHour >= NightStartHour && localHour < NightEndHour;
    }

    private static ServiceResult<AlertDto> NotFound()
    {
        return ServiceResult<AlertDto>.Fail(ServiceErrorType.NotFound, "La alerta solicitada no existe.");
    }
}
