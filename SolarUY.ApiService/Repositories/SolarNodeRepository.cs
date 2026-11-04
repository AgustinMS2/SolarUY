using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class SolarNodeRepository : ISolarNodeRepository
{
    private readonly SolarUyDbContext _context;

    public SolarNodeRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public bool ExistsByName(Guid parkId, string normalizedName)
    {
        return _context.SolarNodes.Any(n => n.SolarParkId == parkId && n.NormalizedName == normalizedName);
    }

    public void Add(SolarNode node)
    {
        _context.SolarNodes.Add(node);
        _context.SaveChanges();
    }

    public SolarNode? GetById(Guid id)
    {
        return _context.SolarNodes.Find(id);
    }

    public IReadOnlyList<SolarNode> GetAll(Guid? parkId = null)
    {
        var query = _context.SolarNodes.Include(n => n.SolarPark).AsQueryable();

        if (parkId.HasValue)
        {
            query = query.Where(n => n.SolarParkId == parkId.Value);
        }

        return query
            .OrderBy(n => n.SolarPark.NormalizedName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(n => n.NormalizedName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}