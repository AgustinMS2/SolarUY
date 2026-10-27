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

    public bool ExistsByNormalizedName(string normalizedName, Guid excludeId)
    {
        return _context.SolarParks.Any(p => p.NormalizedName == normalizedName && p.Id != excludeId);
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

    public SolarPark? GetById(Guid id)
    {
        return _context.SolarParks.Find(id);
    }

    public void Update(SolarPark park)
    {
        _context.SolarParks.Update(park);
        _context.SaveChanges();
    }
}
