using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class SolarUyDomainContextRetriever : ISolarUyDomainContextRetriever
{
    private readonly ISolarParkRepository _parkRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IMaintenanceRepository _maintenanceRepo;
    private readonly IParkGenerationRepository _generationRepo;

    public SolarUyDomainContextRetriever(
        ISolarParkRepository parkRepo,
        IAlertRepository alertRepo,
        IMaintenanceRepository maintenanceRepo,
        IParkGenerationRepository generationRepo)
    {
        _parkRepo = parkRepo;
        _alertRepo = alertRepo;
        _maintenanceRepo = maintenanceRepo;
        _generationRepo = generationRepo;
    }

    public SolarUyDomainSnapshot GetCurrentSnapshot()
    {
        var nowUruguay = DateTime.UtcNow.AddHours(-3);
        var today = DateOnly.FromDateTime(nowUruguay);
        var startOfMonth = new DateOnly(today.Year, today.Month, 1);

        // Parques
        var parks = _parkRepo.GetAll()
            .Select(p => new DomainParkInfo(
                p.Id,
                p.Name,
                p.MaxCapacityKw,
                p.Status == SolarParkStatus.Active ? "Activo" : "Inactivo"))
            .ToList();

        // Alertas
        var recentAlerts = _alertRepo.GetRecent(50);
        var pendingAlerts = recentAlerts.Where(a => a.Status == AlertStatus.Pending).ToList();
        var ackAlerts = recentAlerts.Where(a => a.Status == AlertStatus.Acknowledged).ToList();
        var alertMsgs = pendingAlerts.Take(5).Select(a => $"[{a.Severity}] {a.Description} ({a.ParkName})").ToList();
        var alertInfo = new DomainAlertInfo(pendingAlerts.Count, ackAlerts.Count, alertMsgs);

        // Mantenimientos (orden ascendente: los 5 más próximos primero)
        var pendingMaintenances = _maintenanceRepo.GetAll(new MaintenanceFilter { Status = MaintenanceStatus.Pending });
        var upcoming = pendingMaintenances
            .OrderBy(m => m.ScheduledDateUtc)
            .Take(5)
            .Select(m =>
            {
                var dateStr = m.ScheduledDateUtc.AddHours(-3).ToString("dd/MM HH:mm");
                var tech = m.Technician != null ? m.Technician.FullName : "Sin técnico asignado";
                var park = m.SolarPanel?.SolarPark?.Name ?? "Parque";
                return $"Tipo: {m.Type} en {park} ({dateStr}, Técnico: {tech})";
            }).ToList();
        var maintenanceInfo = new DomainMaintenanceInfo(pendingMaintenances.Count, upcoming);

        // Generaciones
        var genInfoList = new List<DomainGenerationInfo>();
        foreach (var park in parks.Where(p => p.Status == "Activo"))
        {
            var records = _generationRepo.GetByParkAndDateRange(park.Id, startOfMonth, today);
            var todayKwh = records.Where(r => r.DateLocal == today).Sum(r => r.EnergyGeneratedKwh);
            var monthKwh = records.Sum(r => r.EnergyGeneratedKwh);

            genInfoList.Add(new DomainGenerationInfo(
                park.Id,
                park.Name,
                Math.Round(todayKwh, 1),
                Math.Round(monthKwh, 1)));
        }

        return new SolarUyDomainSnapshot(parks, alertInfo, maintenanceInfo, genInfoList);
    }
}
