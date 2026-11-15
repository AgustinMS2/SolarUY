using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Reglas de las alertas de falla (Spec 031): registro con deduplicado y consolidación, estados,
/// contador de la campana y purga.
/// </summary>
public interface IAlertService
{
    /// <summary>Registra una falla detectada: crea una alerta, la descarta por duplicada o la consolida (RF-1, RF-2, RF-6).</summary>
    AlertRegistration Register(FaultDetection fault);

    /// <summary>Marca la alerta como Reconocida si estaba Pendiente; el estado es global (RF-4).</summary>
    ServiceResult<AlertDto> Acknowledge(Guid id);

    /// <summary>Descarta una alerta Reconocida (RF-7).</summary>
    ServiceResult<AlertDto> Dismiss(Guid id);

    /// <summary>Alertas más recientes y cantidad de Pendientes (RF-3).</summary>
    AlertListDto GetRecent(int take);

    /// <summary>Cantidad de alertas Pendientes (RF-3).</summary>
    int GetUnreadCount();

    /// <summary>Elimina las alertas Descartadas nocturnas de más de 24 horas y devuelve cuántas eliminó (RF-5).</summary>
    int PurgeDismissed();
}
