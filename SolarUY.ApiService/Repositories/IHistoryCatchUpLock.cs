namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Candado entre copias del servidor para completar la historia de la simulación (spec 036): mientras una copia
/// completa la historia, las demás no lo hacen a la vez, para no competir por los mismos registros.
/// </summary>
public interface IHistoryCatchUpLock
{
    /// <summary>Toma el candado; devuelve <c>null</c> si otra copia lo tiene. Se libera al desechar lo devuelto.</summary>
    IDisposable? TryAcquire();
}
