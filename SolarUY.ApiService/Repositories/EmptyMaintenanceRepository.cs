using SolarUY.ApiService.Domain;

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
}
