using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class PanelControlEventRepository : IPanelControlEventRepository
{
    private readonly SolarUyDbContext _context;

    public PanelControlEventRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public void Add(PanelControlEvent controlEvent)
    {
        _context.PanelControlEvents.Add(controlEvent);
        _context.SaveChanges();
    }
}
