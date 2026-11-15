using SolarUY.ApiService.Domain;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

public class ParkHealthSnapshotBuilder : IParkHealthSnapshotBuilder
{
    private const string NoNodeGroupName = "Sin nodo asignado";

    private readonly ISolarParkRepository _parkRepository;
    private readonly ISolarPanelRepository _panelRepository;
    private readonly IMaintenanceRepository _maintenanceRepository;
    private readonly IPanelHealthService _healthService;
    private readonly ISolarSimulationEngine _engine;
    private readonly IOptions<SimulationOptions> _options;
    private readonly TimeProvider _timeProvider;

    public ParkHealthSnapshotBuilder(
        ISolarParkRepository parkRepository,
        ISolarPanelRepository panelRepository,
        IMaintenanceRepository maintenanceRepository,
        IPanelHealthService healthService,
        ISolarSimulationEngine engine,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider)
    {
        _parkRepository = parkRepository;
        _panelRepository = panelRepository;
        _maintenanceRepository = maintenanceRepository;
        _healthService = healthService;
        _engine = engine;
        _options = options;
        _timeProvider = timeProvider;
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

        // Carga inicial: las lecturas del ciclo en curso, las mismas que las demás pantallas en vivo.
        var instant = SimulationClock.Snap(_timeProvider.GetUtcNow().UtcDateTime, _options.Value.IntervalSeconds);
        var readings = panels.ToDictionary(p => p.Id, p => ReadPanel(p, instant));

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
            TimestampUtc = instant,
            Summary = _healthService.BuildSummary(readings.Values.Select(r => r.State)),
            Nodes = nodes
        });
    }

    public IReadOnlyList<ParkHealthUpdateDto> BuildUpdatesForActiveParks(DateTime instantUtc)
    {
        var activeParkIds = _parkRepository.GetAll()
            .Where(p => p.Status == SolarParkStatus.Active)
            .Select(p => p.Id)
            .ToHashSet();

        return _panelRepository.GetAll()
            .Where(p => activeParkIds.Contains(p.SolarParkId))
            .GroupBy(p => p.SolarParkId)
            .Select(group =>
            {
                var readings = group.Select(p => ReadPanel(p, instantUtc)).ToList();
                return new ParkHealthUpdateDto
                {
                    ParkId = group.Key,
                    TimestampUtc = instantUtc,
                    Summary = _healthService.BuildSummary(readings.Select(r => r.State)),
                    Panels = readings
                };
            })
            .ToList();
    }

    private PanelHealthReadingDto ReadPanel(SolarPanel panel, DateTime instantUtc)
    {
        var hasActiveNode = panel.SolarNode is { Status: SolarNodeStatus.Active };
        var activePowerW = Math.Round(_engine.ReadPanelPowerW(panel.Id, panel.SolarParkId, panel.PowerW, instantUtc), 1);

        return _healthService.ClassifyHealth(panel, hasActiveNode, activePowerW, SolarCalendar.ToLocal(instantUtc));
    }
}
