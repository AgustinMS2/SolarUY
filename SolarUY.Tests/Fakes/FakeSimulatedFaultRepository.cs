using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeSimulatedFaultRepository : ISimulatedFaultRepository
{
    /// <summary>Fallas "persistidas" por el fake (spec 037).</summary>
    public List<SimulatedFault> Faults { get; } = [];

    public void Add(SimulatedFault fault)
    {
        Faults.Add(fault);
    }

    public IReadOnlyList<SimulatedFault> GetAll()
    {
        return Faults.OrderBy(f => f.StartedAtUtc).ToList();
    }

    public IReadOnlyList<SimulatedFault> GetActive()
    {
        return Faults.Where(f => f.EndedAtUtc is null).OrderBy(f => f.StartedAtUtc).ToList();
    }

    public IReadOnlyList<SimulatedFault> GetActiveByTarget(SimulatedFaultTarget targetType, Guid targetId)
    {
        return Faults
            .Where(f => f.TargetType == targetType && f.TargetId == targetId && f.EndedAtUtc is null)
            .OrderBy(f => f.StartedAtUtc)
            .ToList();
    }

    public int EndByTarget(SimulatedFaultTarget targetType, Guid targetId, DateTime endedAtUtc)
    {
        var active = Faults.Where(f => f.TargetType == targetType && f.TargetId == targetId && f.EndedAtUtc is null).ToList();
        active.ForEach(f => f.EndedAtUtc = endedAtUtc);
        return active.Count;
    }

    public int EndAll(DateTime endedAtUtc)
    {
        var active = Faults.Where(f => f.EndedAtUtc is null).ToList();
        active.ForEach(f => f.EndedAtUtc = endedAtUtc);
        return active.Count;
    }
}
