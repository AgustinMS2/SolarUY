using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

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
}
