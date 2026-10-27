using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class SolarParkRepository : ISolarParkRepository
{
    private readonly SolarUyDbContext _context;

    public SolarParkRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public bool ExistsByNormalizedName(string normalizedName)
    {
        return _context.SolarParks.Any(p => p.NormalizedName == normalizedName);
    }

    public void Add(SolarPark park)
    {
        _context.SolarParks.Add(park);
        _context.SaveChanges();
    }

    public IReadOnlyList<SolarPark> GetAll()
    {
        return _context.SolarParks.ToList();
    }
}
