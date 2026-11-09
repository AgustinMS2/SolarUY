using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Repositories;

public class MaintenanceRepository : IMaintenanceRepository
{
    private readonly SolarUyDbContext _context;

    public MaintenanceRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    // Spec 020 (RF-2): pendientes o programados en el futuro.
    public int CountPendingByTechnician(Guid technicianId)
    {
        var nowUtc = DateTime.UtcNow;
        return _context.MaintenanceOrders.Count(o =>
            o.TechnicianId == technicianId &&
            o.Status == MaintenanceStatus.Pending &&
            o.ScheduledDateUtc > nowUtc);
    }

    public void Add(MaintenanceOrder order)
    {
        _context.MaintenanceOrders.Add(order);
        _context.SaveChanges();
    }

    // Spec 021 (RF-3): día calendario uruguayo (UTC-3 constante).
    public bool HasPendingForPanel(Guid panelId, DateTime dateUtc)
    {
        var localDay = dateUtc.AddHours(-3).Date;
        var startUtc = localDay.AddHours(3);
        var endUtc = startUtc.AddHours(24);

        return _context.MaintenanceOrders.Any(o =>
            o.SolarPanelId == panelId &&
            o.Status == MaintenanceStatus.Pending &&
            o.ScheduledDateUtc >= startUtc &&
            o.ScheduledDateUtc < endUtc);
    }

    // Spec 022: para precargar la edición se necesita el nombre técnico y la serie del panel.
    public MaintenanceOrder? GetById(Guid orderId)
    {
        return _context.MaintenanceOrders
            .Include(o => o.Technician)
            .Include(o => o.SolarPanel)
                .ThenInclude(p => p!.SolarPark)
            .FirstOrDefault(o => o.Id == orderId);
    }

    public void Update(MaintenanceOrder order)
    {
        _context.MaintenanceOrders.Update(order);
        _context.SaveChanges();
    }

    // Spec 024 (RF-2/RF-3): filtros combinables y orden cronológico descendente.
    public IReadOnlyList<MaintenanceOrder> GetAll(MaintenanceFilter filter)
    {
        var query = _context.MaintenanceOrders
            .Include(o => o.Technician)
            .Include(o => o.SolarPanel)
                .ThenInclude(p => p!.SolarPark)
            .AsQueryable();

        if (filter.Status.HasValue)
        {
            query = query.Where(o => o.Status == filter.Status.Value);
        }

        if (filter.TechnicianId.HasValue)
        {
            query = query.Where(o => o.TechnicianId == filter.TechnicianId.Value);
        }

        if (filter.ParkId.HasValue)
        {
            query = query.Where(o => o.SolarPanel!.SolarParkId == filter.ParkId.Value);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(o => o.ScheduledDateUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(o => o.ScheduledDateUtc <= filter.ToUtc.Value);
        }

        return query
            .OrderByDescending(o => o.ScheduledDateUtc)
            .ToList();
    }
}
