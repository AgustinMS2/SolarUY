using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Bitácora de órdenes de control sobre paneles (spec 032).
/// </summary>
public interface IPanelControlEventRepository
{
    /// <summary>
    /// Persiste un evento de control (activación o desactivación) con usuario y fecha UTC (RF-2).
    /// </summary>
    void Add(PanelControlEvent controlEvent);
}
