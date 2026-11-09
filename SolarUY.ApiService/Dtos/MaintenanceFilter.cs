using SolarUY.ApiService.Domain;
using Microsoft.AspNetCore.Mvc;

namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Filtros combinables del listado de mantenimientos (spec 024, RF-3).
/// Atributos de carpeta para el binding de query string en el endpoint GET.
/// </summary>
public class MaintenanceFilter
{
    [FromQuery(Name = "status")]
    public MaintenanceStatus? Status { get; set; }

    [FromQuery(Name = "parkId")]
    public Guid? ParkId { get; set; }

    [FromQuery(Name = "techId")]
    public Guid? TechnicianId { get; set; }

    [FromQuery(Name = "from")]
    public DateTime? FromUtc { get; set; }

    [FromQuery(Name = "to")]
    public DateTime? ToUtc { get; set; }
}
