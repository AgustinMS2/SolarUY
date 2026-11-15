using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class ParkGenerationRepository : IParkGenerationRepository
{
    private readonly SolarUyDbContext _context;

    public ParkGenerationRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<ParkHourlyGeneration> GetByParkAndDateRange(Guid parkId, DateOnly fromDate, DateOnly toDate)
    {
        return _context.ParkHourlyGenerations
            .Where(g => g.SolarParkId == parkId && g.DateLocal >= fromDate && g.DateLocal <= toDate)
            .OrderBy(g => g.DateLocal)
            .ThenBy(g => g.HourLocal)
            .ToList();
    }

    public void AddRange(IEnumerable<ParkHourlyGeneration> records)
    {
        _context.ParkHourlyGenerations.AddRange(records);
        _context.SaveChanges();
    }

    public bool HasAnyForPark(Guid parkId)
    {
        return _context.ParkHourlyGenerations.Any(g => g.SolarParkId == parkId);
    }
}

