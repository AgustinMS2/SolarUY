using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Reglas puras del mapa de salud de paneles (Spec 030): clasificación por generación contra lo
/// esperado a la hora actual, contadores de la cabecera y último mantenimiento de cada panel.
/// </summary>
public interface IPanelHealthService
{
    /// <param name="hasActiveNode">Indica si el nodo del panel existe y está activo.</param>
    PanelHealthReadingDto ClassifyHealth(SolarPanel panel, bool hasActiveNode, double activePowerW, DateTime localTime);

    ParkHealthSummaryDto BuildSummary(IEnumerable<PanelHealthState> states);

    /// <summary>Por panel, la fecha de cierre más reciente entre sus órdenes Completadas.</summary>
    IReadOnlyDictionary<Guid, DateTime> LatestCompletedByPanel(IEnumerable<MaintenanceOrder> orders);
}
