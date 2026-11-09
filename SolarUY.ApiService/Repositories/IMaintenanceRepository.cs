namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Acceso a la cuenta de mantenimientos pendientes por técnico (spec 020, RF-2).
/// Placeholder hasta la spec 021 (Reservar fecha de mantenimiento), cuando exista
/// la entidad Maintenance en el DbContext.
/// </summary>
public interface IMaintenanceRepository
{
    /// <summary>
    /// Devuelve la cantidad de mantenimientos pendientes o programados a futuro
    /// asignados al técnico indicado.
    /// </summary>
    int CountPendingByTechnician(Guid technicianId);
}
