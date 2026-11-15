using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class SimulatedFaultRepository : ISimulatedFaultRepository
{
    private readonly SolarUyDbContext _context;

    public SimulatedFaultRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public void Add(SimulatedFault fault)
    {
        // Npgsql solo acepta fechas con Kind UTC para timestamptz.
        fault.StartedAtUtc = DateTime.SpecifyKind(fault.StartedAtUtc, DateTimeKind.Utc);
        if (fault.EndedAtUtc.HasValue)
        {
            fault.EndedAtUtc = DateTime.SpecifyKind(fault.EndedAtUtc.Value, DateTimeKind.Utc);
        }

        _context.SimulatedFaults.Add(fault);
        _context.SaveChanges();
    }

    public IReadOnlyList<SimulatedFault> GetAll()
    {
        return _context.SimulatedFaults
            .AsNoTracking()
            .OrderBy(f => f.StartedAtUtc)
            .ToList();
    }

    public IReadOnlyList<SimulatedFault> GetActive()
    {
        return _context.SimulatedFaults
            .AsNoTracking()
            .Where(f => f.EndedAtUtc == null)
            .OrderBy(f => f.StartedAtUtc)
            .ToList();
    }

    public IReadOnlyList<SimulatedFault> GetActiveByTarget(SimulatedFaultTarget targetType, Guid targetId)
    {
        return _context.SimulatedFaults
            .AsNoTracking()
            .Where(f => f.TargetType == targetType && f.TargetId == targetId && f.EndedAtUtc == null)
            .OrderBy(f => f.StartedAtUtc)
            .ToList();
    }

    public int EndByTarget(SimulatedFaultTarget targetType, Guid targetId, DateTime endedAtUtc)
    {
        var ended = DateTime.SpecifyKind(endedAtUtc, DateTimeKind.Utc);

        return _context.SimulatedFaults
            .Where(f => f.TargetType == targetType && f.TargetId == targetId && f.EndedAtUtc == null)
            .ExecuteUpdate(s => s.SetProperty(f => f.EndedAtUtc, ended));
    }

    public int EndAll(DateTime endedAtUtc)
    {
        var ended = DateTime.SpecifyKind(endedAtUtc, DateTimeKind.Utc);

        return _context.SimulatedFaults
            .Where(f => f.EndedAtUtc == null)
            .ExecuteUpdate(s => s.SetProperty(f => f.EndedAtUtc, ended));
    }
}
