using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Bitácora inmutable de órdenes de inyección a la red (spec 033). Solo altas, sin edición ni borrado.
/// </summary>
public interface IGridInjectionEventRepository
{
    /// <summary>
    /// Persiste un evento de inyección (corte o reanudación) con usuario, fecha UTC, IP y motivo (RF-2).
    /// </summary>
    void Add(GridInjectionEvent injectionEvent);
}
