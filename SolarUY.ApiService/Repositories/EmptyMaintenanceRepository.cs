namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Implementación placeholder de IMaintenanceRepository mientras no exista la
/// entidad Maintenance en el dominio (spec 021). Reporta 0 pendientes para
/// cualquier técnico; al implementarse la capa de mantenimientos se reemplaza
/// por la consulta real contra la base de datos.
/// </summary>
public class EmptyMaintenanceRepository : IMaintenanceRepository
{
    public int CountPendingByTechnician(Guid technicianId)
    {
        return 0;
    }
}
