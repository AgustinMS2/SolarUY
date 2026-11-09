namespace SolarUY.ApiService.Domain;

// Spec 021: orden de mantenimiento agregable a un panel solar con técnico asignado.
public class MaintenanceOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SolarPanelId { get; set; }

    public SolarPanel? SolarPanel { get; set; }

    public Guid TechnicianId { get; set; }

    public Technician? Technician { get; set; }

    public MaintenanceType Type { get; set; }

    public DateTime ScheduledDateUtc { get; set; }

    public string Description { get; set; } = string.Empty;

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Pending;

    public string? CancellationReason { get; set; }

    public string? ResolutionNotes { get; set; }

    public string? ReplacedParts { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}

public enum MaintenanceType
{
    Preventive = 1,
    Corrective = 2
}

public enum MaintenanceStatus
{
    Pending = 1,
    Completed = 2,
    Cancelled = 3
}
