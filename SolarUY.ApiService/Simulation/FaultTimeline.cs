using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Simulation;

/// <summary>Período en que una falla estuvo (o está) activa en un equipo: inicio incluido y fin excluido (nulo si sigue activa).</summary>
public readonly record struct FaultWindow(Guid TargetId, SimulatedFaultType Type, DateTime StartUtc, DateTime? EndUtc);

/// <summary>
/// Línea de tiempo de fallas simuladas (spec 037). Es inmutable y pura: el simulador la consulta por equipo e instante, de
/// modo que las lecturas en vivo, la historia y la energía del día coincidan. Vacía, el simulador se comporta como en la spec 036.
/// </summary>
public sealed class FaultTimeline
{
    public static readonly FaultTimeline Empty = new([]);

    private readonly Dictionary<Guid, FaultWindow[]> _byTarget;

    public FaultTimeline(IEnumerable<FaultWindow> windows)
    {
        _byTarget = windows
            .Where(w => w.EndUtc is null || w.EndUtc.Value > w.StartUtc)
            .GroupBy(w => w.TargetId)
            .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public bool IsEmpty => _byTarget.Count == 0;

    /// <summary>Arma la línea de tiempo con las fallas guardadas (activas y terminadas).</summary>
    public static FaultTimeline From(IEnumerable<SimulatedFault> faults)
        => new(faults.Select(f => new FaultWindow(f.TargetId, f.FaultType, f.StartedAtUtc, f.EndedAtUtc)));

    /// <summary>
    /// Falla que rige al equipo en el instante dado: de las activas (inicio incluido, fin excluido), la de mayor severidad y,
    /// si empatan, la más reciente. Nulo si no tiene ninguna.
    /// </summary>
    public SimulatedFaultType? EffectiveFault(Guid targetId, DateTime utc)
    {
        if (_byTarget.Count == 0 || !_byTarget.TryGetValue(targetId, out var windows))
        {
            return null;
        }

        var ticks = utc.Ticks;
        FaultWindow? best = null;
        foreach (var window in windows)
        {
            if (ticks < window.StartUtc.Ticks || (window.EndUtc is { } end && ticks >= end.Ticks))
            {
                continue;
            }

            if (best is null || Outranks(window, best.Value))
            {
                best = window;
            }
        }

        return best?.Type;
    }

    private static bool Outranks(FaultWindow candidate, FaultWindow current)
    {
        var candidateSeverity = FaultCatalog.SeverityOf(candidate.Type);
        var currentSeverity = FaultCatalog.SeverityOf(current.Type);
        return candidateSeverity > currentSeverity
               || (candidateSeverity == currentSeverity && candidate.StartUtc > current.StartUtc);
    }
}
