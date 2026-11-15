namespace SolarUY.ApiService.Domain;

// Spec 031: alerta de falla generada por la telemetría y mostrada en la campana de la barra superior.
public class Alert
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SolarParkId { get; set; }

    public SolarPark? SolarPark { get; set; }

    // Nombre del parque en el momento de la alerta, para mostrarla sin consultar el parque.
    public string ParkName { get; set; } = string.Empty;

    public AlertEquipmentType EquipmentType { get; set; }

    // Identificador del parque, del nodo o del panel, según EquipmentType.
    public Guid EquipmentId { get; set; }

    // Nombre del equipo en el momento de la alerta (nombre del parque o del nodo, número de serie del panel).
    public string EquipmentName { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; }

    public AlertStatus Status { get; set; } = AlertStatus.Pending;

    public string Description { get; set; } = string.Empty;

    // Más de 1 cuando la alerta consolida varias detecciones (RF-6).
    public int OccurrenceCount { get; set; } = 1;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime LastOccurrenceAtUtc { get; set; }

    public DateTime? AcknowledgedAtUtc { get; set; }

    public DateTime? DismissedAtUtc { get; set; }

    public DateTime? AttendedAtUtc { get; set; }
}

public enum AlertSeverity
{
    Informational = 1,
    Warning = 2,
    Critical = 3
}

public enum AlertEquipmentType
{
    Park = 1,
    Node = 2,
    Panel = 3
}

public enum AlertStatus
{
    /// <summary>Alerta nueva que nadie reconoció; es la única que cuenta en la campana.</summary>
    Pending = 1,

    /// <summary>Reconocida por algún usuario (estado global).</summary>
    Acknowledged = 2,

    /// <summary>Descartada por un Administrador; queda para la purga.</summary>
    Dismissed = 3,

    /// <summary>La spec 026 la marca así al completar un mantenimiento del panel.</summary>
    Attended = 4
}
