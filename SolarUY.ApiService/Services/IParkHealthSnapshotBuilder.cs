using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Arma los datos del mapa de salud de un parque (Spec 030). Solo orquesta: lee parque, paneles y
/// órdenes completadas, toma las lecturas del motor de simulación (spec 036) y delega las reglas en
/// <see cref="IPanelHealthService"/>.
/// </summary>
public interface IParkHealthSnapshotBuilder
{
    /// <summary>Carga inicial de la pantalla: paneles agrupados por nodo con su primera lectura.</summary>
    ServiceResult<ParkHealthSnapshotDto> BuildSnapshot(Guid parkId);

    /// <summary>
    /// Un evento por cada parque activo, para emitir por SignalR en cada ciclo, con las lecturas del instante dado
    /// (el mismo en que emiten las demás pantallas en vivo).
    /// </summary>
    IReadOnlyList<ParkHealthUpdateDto> BuildUpdatesForActiveParks(DateTime instantUtc);
}
