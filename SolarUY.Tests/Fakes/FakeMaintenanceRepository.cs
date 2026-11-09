using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeMaintenanceRepository : IMaintenanceRepository
{
    /// <summary>
    /// Cantidad de mantenimientos pendientes que reporta el fake para cualquier técnico.
    /// </summary>
    public int PendingCount { get; set; }

    public int CountPendingByTechnician(Guid technicianId)
    {
        return PendingCount;
    }
}
