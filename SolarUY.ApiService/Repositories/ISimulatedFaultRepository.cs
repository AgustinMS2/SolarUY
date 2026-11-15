using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

/// <summary>Acceso a las fallas simuladas (spec 037).</summary>
public interface ISimulatedFaultRepository
{
    void Add(SimulatedFault fault);

    /// <summary>Todas las fallas, activas y terminadas: con ellas se arma la línea de tiempo del simulador.</summary>
    IReadOnlyList<SimulatedFault> GetAll();

    /// <summary>Fallas que todavía no terminaron (sin fecha de fin).</summary>
    IReadOnlyList<SimulatedFault> GetActive();

    /// <summary>Fallas activas de un equipo.</summary>
    IReadOnlyList<SimulatedFault> GetActiveByTarget(SimulatedFaultTarget targetType, Guid targetId);

    /// <summary>Termina todas las fallas activas del equipo en el instante dado y devuelve cuántas terminó.</summary>
    int EndByTarget(SimulatedFaultTarget targetType, Guid targetId, DateTime endedAtUtc);

    /// <summary>Termina todas las fallas activas en el instante dado y devuelve cuántas terminó.</summary>
    int EndAll(DateTime endedAtUtc);
}
