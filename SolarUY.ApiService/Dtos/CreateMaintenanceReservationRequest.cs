using SolarUY.ApiService.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SolarUY.ApiService.Dtos;

// Spec 021: solicitud de reserva de fecha de mantenimiento (RF-1 a RF-3).
public class CreateMaintenanceReservationRequest
{
    [Required(ErrorMessage = "Debés seleccionar un panel solar activo.")]
    [JsonRequired]
    public Guid SolarPanelId { get; set; }

    [Required(ErrorMessage = "Debés seleccionar un técnico activo.")]
    [JsonRequired]
    public Guid TechnicianId { get; set; }

    [JsonRequired]
    public MaintenanceType Type { get; set; }

    [JsonRequired]
    public DateTime ScheduledDateUtc { get; set; }

    [Required(ErrorMessage = "La descripción del trabajo debe contener al menos 5 caracteres.")]
    [StringLength(500, ErrorMessage = "La descripción del trabajo no puede superar los 500 caracteres.")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// RF-3: el Administrador confirmó agendar una segunda intervención para
    /// el mismo panel en el mismo día.
    /// </summary>
#pragma warning disable S6964 // Confirmación opcional: si el cliente no la envía vale false (no confirmó)
    public bool ConfirmSecondIntervention { get; set; }
#pragma warning restore S6964
}
