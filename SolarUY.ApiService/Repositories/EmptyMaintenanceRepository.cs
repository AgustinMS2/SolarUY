using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Fallback de IMaintenanceRepository usado solo en el constructor por defecto
/// de TechnicianService (specs 018-020). Reporta 0 pendientes y no conserva
/// órdenes. El repositorio real (spec 021) es MaintenanceRepository.
/// </summary>
public class EmptyMaintenanceRepository : IMaintenanceRepository
{
    public int CountPendingByTechnician(Guid technicianId)
    {
        return 0;
    }

    public void Add(MaintenanceOrder order)
    {
        // No conserva la orden: este stub no tiene almacenamiento.
    }

    public bool HasPendingForPanel(Guid panelId, DateTime dateUtc)
    {
        return false;
    }

    public MaintenanceOrder? GetById(Guid orderId)
    {
        // Este stub no tiene almacenamiento: no hay órdenes que recuperar.
        return null;
    }

    public void Update(MaintenanceOrder order)
    {
        // No conserva la orden: este stub no tiene almacenamiento.
    }

    public IReadOnlyList<MaintenanceOrder> GetAll(MaintenanceFilter filter)
    {
        // Este stub no tiene almacenamiento: lista siempre vacía.
        return [];
    }
}
