namespace SolarUY.ApiService.Services;

/// <summary>
/// Órdenes remotas de activación/desactivación eléctrica de un panel (spec 032).
/// </summary>
public interface IPanelControlService
{
    /// <summary>RF-1/RF-2: corte eléctrico remoto del panel, con bitácora.</summary>
    ServiceResult Deactivate(Guid panelId, string orderedBy);

    /// <summary>RF-3/RF-4: reactivación del panel, con bloqueo RF-5 por mantenimiento pendiente.</summary>
    ServiceResult Activate(Guid panelId, string orderedBy);
}
