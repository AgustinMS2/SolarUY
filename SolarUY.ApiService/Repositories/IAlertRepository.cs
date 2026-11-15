using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

/// <summary>
/// Acceso a las alertas de falla (spec 031).
/// </summary>
public interface IAlertRepository
{
    void Add(Alert alert);

    void Update(Alert alert);

    void RemoveRange(IReadOnlyCollection<Alert> alerts);

    Alert? GetById(Guid id);

    /// <summary>
    /// Última alerta de la misma severidad y equipo, en cualquier estado (deduplicado, RF-2).
    /// </summary>
    Alert? GetLatest(AlertSeverity severity, AlertEquipmentType equipmentType, Guid equipmentId);

    /// <summary>
    /// Alertas Pendientes o Reconocidas de la misma severidad y equipo cuya última detección es
    /// posterior o igual a <paramref name="sinceUtc"/> (consolidación, RF-6).
    /// </summary>
    IReadOnlyList<Alert> GetOpenSince(AlertSeverity severity, AlertEquipmentType equipmentType, Guid equipmentId, DateTime sinceUtc);

    /// <summary>
    /// Alertas más recientes primero, por última detección.
    /// </summary>
    IReadOnlyList<Alert> GetRecent(int take);

    int CountByStatus(AlertStatus status);

    /// <summary>
    /// Alertas Pendientes o Reconocidas de un panel (spec 026, RF-3).
    /// </summary>
    IReadOnlyList<Alert> GetActiveByPanel(Guid panelId);

    /// <summary>
    /// Alertas Descartadas creadas antes de la fecha dada (candidatas a la purga, RF-5).
    /// </summary>
    IReadOnlyList<Alert> GetDismissedCreatedBefore(DateTime createdBeforeUtc);
}
