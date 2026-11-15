using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;

namespace SolarUY.ApiService.Services;

public class ParkHealthSnapshotBuilder : IParkHealthSnapshotBuilder
{
    private const string NoNodeGroupName = "Sin nodo asignado";

    private readonly ISolarParkRepository _parkRepository;
    private readonly ISolarPanelRepository _panelRepository;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPanelHealthService _healthService;
    private readonly IPanelTelemetryService _telemetryService;

    public ParkHealthSnapshotBuilder(
        ISolarParkRepository parkRepository,
        ISolarPanelRepository panelRepository,
        IMaintenanceRepository maintenanceRepository,
        IPanelHealthService healthService,
        IPanelTelemetryService telemetryService)
    {
        _parkRepository = parkRepository;
        _panelRepository = panelRepository;
        _maintenanceRepository = maintenanceRepository;
        _healthService = healthService;
        _telemetryService = telemetryService;
    }

    public ServiceResult<ParkHealthSnapshotDto> BuildSnapshot(Guid parkId)
    {
        var park = _parkRepository.GetById(parkId);
        if (park is null)
        {
            return ServiceResult<ParkHealthSnapshotDto>.Fail(ServiceErrorType.NotFound, "El parque solicitado no existe.");
        }

        var panels = _panelRepository.GetAll().Where(p => p.SolarParkId == parkId).ToList();
        var completedOrders = _maintenanceRepository.GetAll(new MaintenanceFilter
        {
            Status = MaintenanceStatus.Completed,
            ParkId = parkId
        });
        var lastMaintenance = _healthService.LatestCompletedByPanel(completedOrders);

        var localHour = GetUruguayLocalHour();
        var readings = panels.ToDictionary(p => p.Id, p => ReadPanel(p, localHour));

        var nodes = panels
            .GroupBy(p => p.SolarNodeId)
            .Select(group => new ParkHealthNodeDto
            {
                NodeId = group.Key,
                NodeName = group.Key is null ? NoNodeGroupName : group.First().SolarNode?.Name ?? NoNodeGroupName,
                Panels = group
                    .OrderBy(p => p.SerieNumber, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new ParkHealthPanelDto
                    {
                        PanelId = p.Id,
                        SerieNumber = p.SerieNumber,
                        NominalPowerW = p.PowerW,
                        NominalEfficiencyPercent = p.Efficiency,
                        LastMaintenanceUtc = lastMaintenance.TryGetValue(p.Id, out var date) ? date : null,
                        Reading = readings[p.Id]
                    })
                    .ToList()
            })
            .OrderBy(n => n.NodeId is null)
            .ThenBy(n => n.NodeName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ServiceResult<ParkHealthSnapshotDto>.Ok(new ParkHealthSnapshotDto
        {
            ParkId = park.Id,
            ParkName = park.Name,
            TimestampUtc = DateTime.UtcNow,
            Summary = _healthService.BuildSummary(readings.Values.Select(r => r.State)),
            Nodes = nodes
        });
    }

    public IReadOnlyList<ParkHealthUpdateDto> BuildUpdatesForActiveParks()
    {
        var activeParkIds = _parkRepository.GetAll()
            .Where(p => p.Status == SolarParkStatus.Active)
            .Select(p => p.Id)
            .ToHashSet();

        var localHour = GetUruguayLocalHour();
        var timestamp = DateTime.UtcNow;

        return _panelRepository.GetAll()
            .Where(p => activeParkIds.Contains(p.SolarParkId))
            .GroupBy(p => p.SolarParkId)
            .Select(group =>
            {
                var readings = group.Select(p => ReadPanel(p, localHour)).ToList();
                return new ParkHealthUpdateDto
                {
                    ParkId = group.Key,
                    TimestampUtc = timestamp,
                    Summary = _healthService.BuildSummary(readings.Select(r => r.State)),
                    Panels = readings
                };
            })
            .ToList();
    }

    private PanelHealthReadingDto ReadPanel(SolarPanel panel, double localHour)
    {
        var hasActiveNode = panel.SolarNode is { Status: SolarNodeStatus.Active };
        var expectedPowerW = _telemetryService.CalculateExpectedPowerW(panel.PowerW, localHour);
        var activePowerW = PanelReadingSimulator.SimulateActivePowerW(expectedPowerW);

        return _healthService.ClassifyHealth(panel, hasActiveNode, activePowerW, localHour);
    }

    private static double GetUruguayLocalHour()
    {
        var local = GetUruguayLocalTime();
        return local.Hour + (local.Minute / 60.0);
    }

    private static DateTime GetUruguayLocalTime()
    {
        foreach (var id in new[] { "America/Montevideo", "Montevideo Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var tz) && tz != null)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            }
        }

        return DateTime.UtcNow.AddHours(-3);
    }
}
