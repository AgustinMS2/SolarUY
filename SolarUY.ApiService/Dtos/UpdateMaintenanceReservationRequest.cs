using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

// Spec 022: modificación de una reserva de mantenimiento en estado Pendiente.
// El panel solar no es editable en esta spec (fuera de alcance de reasignación de panel).
public class UpdateMaintenanceReservationRequest
{
    [Required(ErrorMessage = "Debés seleccionar un técnico activo.")]
    [JsonRequired]
    public Guid TechnicianId { get; set; }

    [JsonRequired]
    public DateTime ScheduledDateUtc { get; set; }

    [Required(ErrorMessage = "La descripción del trabajo debe contener al menos 5 caracteres.")]
    [StringLength(500, ErrorMessage = "La descripción del trabajo no puede superar los 500 caracteres.")]
    public string Description { get; set; } = string.Empty;
}
