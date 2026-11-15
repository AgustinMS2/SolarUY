using System.Buffers.Binary;

namespace SolarUY.ApiService.Simulation;

/// <summary>
/// Ruido reproducible para la simulación (Spec 036): a partir de una semilla (un equipo o un parque) y un instante
/// siempre da el mismo valor, sin estado compartido ni azar propio de cada instancia. Así dos copias de la API
/// calculan los mismos números. Usa un mezclador de enteros fijo (SplitMix64) y no <c>HashCode</c> ni
/// <c>Random</c>, cuyos resultados cambian de un proceso a otro.
/// </summary>
public static class DeterministicNoise
{
    private const double TwoPow53 = 9007199254740992.0;
    private static readonly long EpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

    /// <summary>Valor uniforme en [0, 1) para (semilla, índice, canal).</summary>
    public static double Unit(Guid seed, long index, int channel = 0)
    {
        return (Hash(seed, index, channel) >> 11) / TwoPow53;
    }

    /// <summary>Valor en [−1, 1] de un punto de control.</summary>
    public static double KnotValue(Guid seed, long knotIndex, int channel = 0)
    {
        return (2.0 * Unit(seed, knotIndex, channel)) - 1.0;
    }

    /// <summary>
    /// Ruido suave en [−1, 1]: interpola con suavizado entre puntos de control separados <paramref name="periodSeconds"/>
    /// segundos, por lo que cambia de forma gradual entre instantes cercanos. Los canales distintos son secuencias independientes.
    /// </summary>
    public static double Smooth(Guid seed, DateTime utc, int periodSeconds, int channel = 0)
    {
        var seconds = (utc.Ticks - EpochTicks) / (double)TimeSpan.TicksPerSecond;
        var position = seconds / periodSeconds;
        var knot = (long)Math.Floor(position);
        var t = position - knot;
        var weight = t * t * (3.0 - (2.0 * t));

        var from = KnotValue(seed, knot, channel);
        var to = KnotValue(seed, knot + 1, channel);
        return from + ((to - from) * weight);
    }

    private static ulong Hash(Guid seed, long index, int channel)
    {
        Span<byte> bytes = stackalloc byte[16];
        seed.TryWriteBytes(bytes, bigEndian: false, out _);
        var low = BinaryPrimitives.ReadUInt64LittleEndian(bytes);
        var high = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..]);

        var salted = unchecked((ulong)index + (0x9E3779B97F4A7C15UL * (ulong)(channel + 1)));
        return Mix(Mix(low ^ Mix(salted)) ^ high);
    }

    // SplitMix64.
    private static ulong Mix(ulong x)
    {
        unchecked
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }
}
