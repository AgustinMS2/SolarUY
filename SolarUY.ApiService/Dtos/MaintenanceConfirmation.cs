namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Resultado de confirmar un mantenimiento (spec 026): las alertas del panel que quedaron como Atendidas (RF-3),
/// para que el controlador avise a los usuarios conectados.
/// </summary>
public record MaintenanceConfirmation(IReadOnlyList<AlertDto> AttendedAlerts);
