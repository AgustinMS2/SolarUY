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
            .ToList();
    }
}
