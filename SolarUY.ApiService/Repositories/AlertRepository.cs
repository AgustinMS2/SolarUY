using Microsoft.EntityFrameworkCore;
using SolarUY.ApiService.Data;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly SolarUyDbContext _context;

    public AlertRepository(SolarUyDbContext context)
    {
        _context = context;
    }

    public void Add(Alert alert)
    {
        _context.Alerts.Add(alert);
        _context.SaveChanges();
    }

    public void Update(Alert alert)
    {
        _context.Alerts.Update(alert);
        _context.SaveChanges();
    }

    public void RemoveRange(IReadOnlyCollection<Alert> alerts)
    {
        if (alerts.Count == 0)
        {
            return;
        }

        _context.Alerts.RemoveRange(alerts);
        _context.SaveChanges();
    }

    public Alert? GetById(Guid id)
    {
        return _context.Alerts.FirstOrDefault(a => a.Id == id);
    }

    public Alert? GetLatest(AlertSeverity severity, AlertEquipmentType equipmentType, Guid equipmentId)
    {
        return _context.Alerts
            .Where(a => a.Severity == severity && a.EquipmentType == equipmentType && a.EquipmentId == equipmentId)
            .OrderByDescending(a => a.LastOccurrenceAtUtc)
            .FirstOrDefault();
    }

    public IReadOnlyList<Alert> GetOpenSince(AlertSeverity severity, AlertEquipmentType equipmentType, Guid equipmentId, DateTime sinceUtc)
    {
        return _context.Alerts
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
        return _context.Alerts
            .OrderByDescending(a => a.LastOccurrenceAtUtc)
            .Take(take)
            .ToList();
    }

    public int CountByStatus(AlertStatus status)
    {
        return _context.Alerts.Count(a => a.Status == status);
    }

    public IReadOnlyList<Alert> GetDismissedCreatedBefore(DateTime createdBeforeUtc)
    {
        return _context.Alerts
            .Where(a => a.Status == AlertStatus.Dismissed && a.CreatedAtUtc < createdBeforeUtc)
            .ToList();
    }
}
