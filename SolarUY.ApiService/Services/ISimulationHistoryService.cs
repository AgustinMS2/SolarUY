namespace SolarUY.ApiService.Services;

/// <summary>Cantidad de registros nuevos que guardó una pasada de la historia.</summary>
public readonly record struct HistoryCatchUpResult(int MinuteRows, int HourRows);

/// <summary>
/// Guarda la historia de la simulación (spec 036): un registro por inversor y minuto y un registro por parque y hora,
/// calculados con el mismo motor que las lecturas en vivo.
/// </summary>
public interface ISimulationHistoryService
{
    /// <summary>
    /// Completa lo que falta hasta el último minuto y la última hora completos: sin historia previa, los últimos
    /// <paramref name="backfillDays"/> días por minuto y desde el 1 de enero del año anterior por hora; con historia,
    /// solo el hueco. Es idempotente: llamarla de nuevo no duplica nada. Guarda en lotes y revisa la cancelación entre
    /// lotes, de modo que lo ya guardado se conserva.
    /// </summary>
    HistoryCatchUpResult CatchUp(DateTime nowUtc, int backfillDays, CancellationToken cancellationToken = default);
}
