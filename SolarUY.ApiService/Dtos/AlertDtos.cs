using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Condición de falla detectada por la telemetría, antes de convertirse en alerta (Spec 031, RF-1).
/// </summary>
public record FaultDetection(
    Guid ParkId,
    string ParkName,
    AlertEquipmentType EquipmentType,
    Guid EquipmentId,
    string EquipmentName,
    AlertSeverity Severity,
    string Description);

/// <summary>Alerta tal como la ven la campana, el centro de alertas y los avisos.</summary>
public record AlertDto(
    Guid Id,
    Guid ParkId,
    string ParkName,
    AlertEquipmentType EquipmentType,
    Guid EquipmentId,
    string EquipmentName,
    AlertSeverity Severity,
    AlertStatus Status,
    string Description,
    int OccurrenceCount,
    DateTime CreatedAtUtc,
    DateTime LastOccurrenceAtUtc,
    DateTime? AcknowledgedAtUtc);

/// <summary>Alertas recientes y contador de Pendientes (RF-3).</summary>
public record AlertListDto(int UnreadCount, IReadOnlyList<AlertDto> Alerts);

public enum AlertRegistrationOutcome
{
    /// <summary>Se creó una alerta nueva.</summary>
    Created = 1,

    /// <summary>Duplicada dentro de la ventana de 60 s: no se crea nada ni se emite evento (RF-2).</summary>
    Discarded = 2,

    /// <summary>Se agrupó con otras alertas en una consolidada (RF-6).</summary>
    Consolidated = 3
}

public record AlertRegistration(AlertRegistrationOutcome Outcome, AlertDto? Alert);

public enum AlertEventKind
{
    /// <summary>Alerta nueva: genera un aviso.</summary>
    New = 1,

    /// <summary>Alerta consolidada que sumó una instancia: genera un aviso.</summary>
    Updated = 2,

    /// <summary>Cambió el estado (reconocida o descartada): solo actualiza la campana y la lista.</summary>
    StatusChanged = 3
}

/// <summary>Evento de SignalR con la alerta y el contador de Pendientes ya actualizado.</summary>
public record AlertEventDto(AlertEventKind Kind, AlertDto Alert, int UnreadCount);
