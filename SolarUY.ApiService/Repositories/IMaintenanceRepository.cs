using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Acceso a órdenes de mantenimiento (spec 021) y a la cuenta de pendientes
/// por técnico usada por la baja de técnico (spec 020, RF-2).
/// </summary>
public interface IMaintenanceRepository
{
    /// <summary>
    /// Devuelve la cantidad de mantenimientos pendientes o programados a futuro
    /// asignados al técnico indicado (spec 020, RF-2).
    /// </summary>
    int CountPendingByTechnician(Guid technicianId);

    /// <summary>
    /// Persiste una nueva orden de mantenimiento (spec 021).
    /// </summary>
    void Add(MaintenanceOrder order);

    /// <summary>
    /// Indica si el panel indicado ya tiene una orden en estado Pendiente
    /// dentro del día calendario uruguayo (UTC-3) de la fecha dada (spec 021, RF-3).
    /// </summary>
    bool HasPendingForPanel(Guid panelId, DateTime dateUtc);
}
