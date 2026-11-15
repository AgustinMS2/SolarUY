using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;

namespace SolarUY.Tests.Fakes;

public class FakeAlertRepository : IAlertRepository
{
    /// <summary>Alertas "persistidas" por el fake (spec 031).</summary>
    public List<Alert> Alerts { get; } = [];

    public void Add(Alert alert)
    {
        Alerts.Add(alert);
    }

    public void Update(Alert alert)
    {
        var index = Alerts.FindIndex(a => a.Id == alert.Id);
        if (index >= 0)
        {
            Alerts[index] = alert;
        }
        else
        {
            Alerts.Add(alert);
        }
    }

    public void RemoveRange(IReadOnlyCollection<Alert> alerts)
    {
        foreach (var alert in alerts)
        {
            Alerts.RemoveAll(a => a.Id == alert.Id);
        }
    }

    public Alert? GetById(Guid id) => Alerts.FirstOrDefault(a => a.Id == id);

    public Alert? GetLatest(AlertSeverity severity, AlertEquipmentType equipmentType, Guid equipmentId)
    {
        return Alerts
            .Where(a => a.Severity == severity && a.EquipmentType == equipmentType && a.EquipmentId == equipmentId)
            .OrderByDescending(a => a.LastOccurrenceAtUtc)
            .FirstOrDefault();
    }

    public IReadOnlyList<Alert> GetOpenSince(AlertSeverity severity, AlertEquipmentType equipmentType, Guid equipmentId, DateTime sinceUtc)
    {
        return Alerts
            .Where(a => a.Severity == severity
                        && a.EquipmentType == equipmentType
                        && a.EquipmentId == equipmentId
                        && a.LastOccurrenceAtUtc >= sinceUtc
                        && (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Acknowledged))
            .OrderBy(a => a.CreatedAtUtc)
            .ToList();
    }

    public IReadOnlyList<Alert> GetRecent(int take)
    {
        return Alerts.OrderByDescending(a => a.LastOccurrenceAtUtc).Take(take).ToList();
    }

    public int CountByStatus(AlertStatus status) => Alerts.Count(a => a.Status == status);

    public IReadOnlyList<Alert> GetDismissedCreatedBefore(DateTime createdBeforeUtc)
    {
        return Alerts.Where(a => a.Status == AlertStatus.Dismissed && a.CreatedAtUtc < createdBeforeUtc).ToList();
    }
}
