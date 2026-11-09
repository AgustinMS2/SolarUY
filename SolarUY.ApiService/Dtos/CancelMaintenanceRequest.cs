using System.ComponentModel.DataAnnotations;

namespace SolarUY.ApiService.Dtos;

// Spec 023: solicitud de cancelación de un mantenimiento pendiente.
public class CancelMaintenanceRequest
{
    [Required(ErrorMessage = "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres.")]
    [MinLength(10, ErrorMessage = "El motivo de cancelación es obligatorio y debe tener al menos 10 caracteres.")]
    [StringLength(500, ErrorMessage = "El motivo de cancelación no puede superar los 500 caracteres.")]
    public string Reason { get; set; } = string.Empty;
}
