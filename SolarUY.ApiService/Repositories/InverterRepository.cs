using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class InverterRepository : IInverterRepository
{
    private readonly SolarUyDbContext _context;

    public InverterRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public Inverter? GetBySerie(string normalizedSerie)
    {
        return _context.Inverters.FirstOrDefault(i => i.NormalizedSerieNumber == normalizedSerie);
    }

    public Inverter? GetByNodeId(Guid nodeId)
    {
        return _context.Inverters.FirstOrDefault(i => i.SolarNodeId == nodeId);
    }

    public IReadOnlyList<Inverter> GetByNodeIds(IReadOnlyCollection<Guid> nodeIds)
    {
        return _context.Inverters.Where(i => nodeIds.Contains(i.SolarNodeId)).ToList();
    }

    public void Add(Inverter inverter)
    {
        _context.Inverters.Add(inverter);
        _context.SaveChanges();
    }

    public void Update(Inverter inverter)
    {
        _context.Inverters.Update(inverter);
        _context.SaveChanges();
    }

    public void Remove(Inverter inverter)
    {
        _context.Inverters.Remove(inverter);
        _context.SaveChanges();
    }
}
