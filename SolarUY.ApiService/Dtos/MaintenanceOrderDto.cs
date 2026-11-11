using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Dtos;

// Spec 021/022: representación de una orden de mantenimiento para lectura/edición.
public class MaintenanceOrderDto
{
    public Guid Id { get; set; }

    public Guid SolarPanelId { get; set; }

    public string? SolarPanelSerie { get; set; }

    public string? SolarParkName { get; set; }

    public string? SolarNodeName { get; set; }

    public Guid TechnicianId { get; set; }

    public string? TechnicianName { get; set; }

    public MaintenanceType Type { get; set; }

    public DateTime ScheduledDateUtc { get; set; }

    public string Description { get; set; } = string.Empty;

    public MaintenanceStatus Status { get; set; }
}
