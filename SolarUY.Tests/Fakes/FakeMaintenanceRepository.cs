using SolarUY.ApiService.Domain;
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
}
