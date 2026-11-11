using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeMaintenanceRepository : IMaintenanceRepository
{
    /// <summary>
    /// Cantidad de mantenimientos pendientes que reporta el fake para cualquier técnico.
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Órdenes de mantenimiento "persistidas" por el fake (spec 021).
    /// </summary>
    public List<MaintenanceOrder> PersistedOrders { get; } = [];

    /// <summary>
    /// Paneles conocidos por el fake, necesarios para resolver el filtro por parque (spec 024).
    /// </summary>
    public List<SolarPanel> Panels { get; } = [];

    public int CountPendingByTechnician(Guid technicianId)
    {
        return PendingCount;
    }

    public void Add(MaintenanceOrder order)
    {
        PersistedOrders.Add(order);
    }

    public bool HasPendingForPanel(Guid panelId, DateTime dateUtc)
    {
        // Día calendario uruguayo (UTC-3 constante, spec 021 RF-3).
        var localDay = dateUtc.AddHours(-3).Date;
        var startUtc = localDay.AddHours(3);
        var endUtc = startUtc.AddHours(24);

        return PersistedOrders.Any(o =>
            o.SolarPanelId == panelId &&
            o.Status == MaintenanceStatus.Pending &&
            o.ScheduledDateUtc >= startUtc &&
            o.ScheduledDateUtc < endUtc);
    }

    public MaintenanceOrder? GetById(Guid orderId)
    {
        return PersistedOrders.FirstOrDefault(o => o.Id == orderId);
    }

    public void Update(MaintenanceOrder order)
    {
        var index = PersistedOrders.FindIndex(o => o.Id == order.Id);
        if (index >= 0)
        {
            PersistedOrders[index] = order;
        }
        else
        {
            PersistedOrders.Add(order);
        }
    }

    public IReadOnlyList<MaintenanceOrder> GetAll(MaintenanceFilter filter)
    {
        var query = PersistedOrders.AsEnumerable();

        if (filter.Status.HasValue)
        {
            var status = filter.Status.Value;
            query = query.Where(o => o.Status == status);
        }

        if (filter.TechnicianId.HasValue)
        {
            var technicianId = filter.TechnicianId.Value;
            query = query.Where(o => o.TechnicianId == technicianId);
        }

        if (filter.ParkId.HasValue)
        {
            var parkId = filter.ParkId.Value;
            var panelIdsOfPark = Panels
                .Where(p => p.SolarParkId == parkId)
                .Select(p => p.Id)
                .ToHashSet();
            query = query.Where(o => panelIdsOfPark.Contains(o.SolarPanelId));
        }

        if (filter.FromUtc.HasValue)
        {
            var fromUtc = filter.FromUtc.Value;
            query = query.Where(o => o.ScheduledDateUtc >= fromUtc);
        }

        if (filter.ToUtc.HasValue)
        {
            var toUtc = filter.ToUtc.Value;
            query = query.Where(o => o.ScheduledDateUtc <= toUtc);
        }

        // Spec 024 (RF-2): orden cronológico descendente por fecha programada.
        return query
            .OrderByDescending(o => o.ScheduledDateUtc)
            .ToList();
    }

    public IReadOnlyList<MaintenanceOrder> GetByTechnicianId(Guid technicianId)
    {
        return PersistedOrders.Where(o => o.TechnicianId == technicianId).ToList();
    }
}
