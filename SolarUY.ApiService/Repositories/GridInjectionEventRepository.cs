using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class GridInjectionEventRepository : IGridInjectionEventRepository
{
    private readonly SolarUyDbContext _context;

    public GridInjectionEventRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public void Add(GridInjectionEvent injectionEvent)
    {
        _context.GridInjectionEvents.Add(injectionEvent);
        _context.SaveChanges();
    }
}
