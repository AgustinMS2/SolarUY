using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class SolarPanelRepository : ISolarPanelRepository
{
    private readonly SolarUyDbContext _context;

    public SolarPanelRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public bool ExistsBySerieNumber(string normalizedSerieNumber)
    {
        return _context.SolarPanels.Any(p => p.NormalizedSerieNumber == normalizedSerieNumber);
    }

    public bool ExistsBySerieNumber(string normalizedSerieNumber, Guid excludeId)
    {
        return _context.SolarPanels.Any(p => p.NormalizedSerieNumber == normalizedSerieNumber && p.Id != excludeId);
    }

    public void Add(SolarPanel panel)
    {
        _context.SolarPanels.Add(panel);
        _context.SaveChanges();
    }

    public SolarPark? GetParkById(Guid parkId)
    {
        return _context.SolarParks.Find(parkId);
    }

    public IReadOnlyList<SolarPanel> GetAll()
    {
        return _context.SolarPanels
            .Include(p => p.SolarPark)
            .Include(p => p.SolarNode)
            .ToList();
    }

    public SolarPanel? GetById(Guid id)
    {
        return _context.SolarPanels
            .Include(p => p.SolarPark)
            .Include(p => p.SolarNode)
            .FirstOrDefault(p => p.Id == id);
    }

    public void Update(SolarPanel panel)
    {
        _context.SolarPanels.Update(panel);
        _context.SaveChanges();
    }

    public IReadOnlyList<SolarPanel> GetByIds(IReadOnlyCollection<Guid> ids)
    {
        return _context.SolarPanels.Where(p => ids.Contains(p.Id)).ToList();
    }

    public IReadOnlyList<SolarPanel> GetByNodeId(Guid nodeId)
    {
        return _context.SolarPanels.Where(p => p.SolarNodeId == nodeId).ToList();
    }

    public IReadOnlyDictionary<Guid, int> CountByNodeIds(IReadOnlyCollection<Guid> nodeIds)
    {
        return _context.SolarPanels
            .Where(p => p.SolarNodeId != null && nodeIds.Contains(p.SolarNodeId.Value))
            .GroupBy(p => p.SolarNodeId!.Value)
            .Select(g => new { NodeId = g.Key, Count = g.Count() })
            .ToDictionary(x => x.NodeId, x => x.Count);
    }

    // One SaveChanges for the whole batch, so a node assignment is all-or-nothing.
    public void UpdateRange(IReadOnlyList<SolarPanel> panels)
    {
        _context.SolarPanels.UpdateRange(panels);
        _context.SaveChanges();
    }
}
