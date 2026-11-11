namespace SolarUY.ApiService.Dtos;

// Spec 026: confirmación de un mantenimiento realizado.
// Los campos son opcionales a nivel de transporte para que las reglas y mensajes exactos los resuelva el servicio.
public class ConfirmMaintenanceRequest
{
    public string? Notes { get; set; }

    public string? ReplacedParts { get; set; }

    // "Operational" (Operativo / Normal) o "NeedsFollowUp" (Requiere seguimiento adicional).
    public string? PanelOutcome { get; set; }
}
