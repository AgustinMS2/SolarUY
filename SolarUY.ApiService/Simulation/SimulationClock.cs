namespace SolarUY.ApiService.Simulation;

/// <summary>
/// Reloj compartido de la simulación (spec 036): todos los servicios en vivo emiten en los mismos instantes, múltiplos
/// del intervalo configurado, de modo que dos pantallas que reciben paquetes del mismo ciclo muestran los mismos números.
/// </summary>
public static class SimulationClock
{
    /// <summary>Margen para despertar un poco después del límite del ciclo y no antes por redondeos del temporizador.</summary>
    public static readonly TimeSpan CycleMargin = TimeSpan.FromMilliseconds(50);

    /// <summary>Instante UTC de la lectura: el dado, redondeado hacia abajo a un múltiplo del intervalo.</summary>
    public static DateTime Snap(DateTime utc, int intervalSeconds)
    {
        var interval = Math.Max(1, intervalSeconds) * TimeSpan.TicksPerSecond;
        return new DateTime(utc.Ticks - (utc.Ticks % interval), DateTimeKind.Utc);
    }

    /// <summary>Tiempo de espera hasta el próximo múltiplo del intervalo (más el margen).</summary>
    public static TimeSpan DelayUntilNextCycle(DateTime utcNow, int intervalSeconds)
    {
        var interval = Math.Max(1, intervalSeconds) * TimeSpan.TicksPerSecond;
        var untilNext = interval - (utcNow.Ticks % interval);
        return TimeSpan.FromTicks(untilNext) + CycleMargin;
    }
}
