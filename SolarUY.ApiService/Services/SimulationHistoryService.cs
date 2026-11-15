using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Repositories;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

public class SimulationHistoryService : ISimulationHistoryService
{
    private const int MinuteBatchSize = 2000;
    private const int HourBatchSize = 1000;

    // Uruguay: UTC−3 todo el año.
    private static readonly TimeSpan UruguayOffset = TimeSpan.FromHours(-3);

    private readonly ISolarParkRepository _parkRepository;
    private readonly ISolarNodeRepository _nodeRepository;
    private readonly IInverterRepository _inverterRepository;
    private readonly IInverterMinuteReadingRepository _minuteRepository;
    private readonly IParkGenerationRepository _hourRepository;
    private readonly ISolarSimulationEngine _engine;
    private readonly IHistoryCatchUpLock _lock;

    public SimulationHistoryService(
        ISolarParkRepository parkRepository,
        ISolarNodeRepository nodeRepository,
        IInverterRepository inverterRepository,
        IInverterMinuteReadingRepository minuteRepository,
        IParkGenerationRepository hourRepository,
        ISolarSimulationEngine engine,
        IHistoryCatchUpLock catchUpLock)
    {
        _parkRepository = parkRepository;
        _nodeRepository = nodeRepository;
        _inverterRepository = inverterRepository;
        _minuteRepository = minuteRepository;
        _hourRepository = hourRepository;
        _engine = engine;
        _lock = catchUpLock;
    }

    public HistoryCatchUpResult CatchUp(DateTime nowUtc, int backfillDays, CancellationToken cancellationToken = default)
    {
        // Si otra copia del servidor está completando la historia, esta espera al próximo minuto: ya habrá terminado
        // y, si quedara algún hueco, lo completa entonces.
        using var gate = _lock.TryAcquire();
        if (gate is null)
        {
            return new HistoryCatchUpResult(0, 0);
        }

        var parks = _parkRepository.GetAll().Where(p => p.Status == SolarParkStatus.Active).ToList();
        if (parks.Count == 0)
        {
            return new HistoryCatchUpResult(0, 0);
        }

        // Inversores de nodos activos de parques activos, agrupados por parque (RF-4).
        var equipment = new List<(SolarPark Park, List<(Inverter Inverter, Guid NodeId)> Inverters)>();
        foreach (var park in parks)
        {
            var nodeIds = _nodeRepository.GetAll(park.Id)
                .Where(n => n.Status == SolarNodeStatus.Active)
                .Select(n => n.Id)
                .ToList();
            var inverters = nodeIds.Count == 0
                ? []
                : _inverterRepository.GetByNodeIds(nodeIds).Select(i => (i, i.SolarNodeId)).ToList();

            if (inverters.Count > 0)
            {
                equipment.Add((park, inverters));
            }
        }

        // Primero las horas: son las que consulta la spec 034, así sus reportes aparecen antes al arrancar.
        var hourRows = SaveHours(equipment, nowUtc, cancellationToken);
        var minuteRows = SaveMinutes(equipment, nowUtc, backfillDays, cancellationToken);
        return new HistoryCatchUpResult(minuteRows, hourRows);
    }

    private int SaveMinutes(
        List<(SolarPark Park, List<(Inverter Inverter, Guid NodeId)> Inverters)> equipment,
        DateTime nowUtc,
        int backfillDays,
        CancellationToken cancellationToken)
    {
        var lastByInverter = _minuteRepository.GetLastMinuteByInverter();
        var batch = new List<InverterMinuteReading>(MinuteBatchSize);
        var saved = 0;

        foreach (var (park, inverters) in equipment)
        {
            foreach (var (inverter, nodeId) in inverters)
            {
                DateTime? last = lastByInverter.TryGetValue(inverter.Id, out var lastMinute) ? lastMinute : null;
                var range = HistoryGapPlanner.MissingMinutes(last, inverter.CreatedAtUtc, nowUtc, backfillDays);
                if (range is null)
                {
                    continue;
                }

                for (var minute = range.Value.FromUtc; minute < range.Value.ToUtc; minute = minute.AddMinutes(1))
                {
                    batch.Add(MinuteAggregator.Build(_engine, inverter.Id, park.Id, nodeId, inverter.NominalCapacityKw, minute));
                    if (batch.Count >= MinuteBatchSize)
                    {
                        saved += FlushMinutes(batch, cancellationToken);
                    }
                }
            }
        }

        return saved + FlushMinutes(batch, cancellationToken);
    }

    private int FlushMinutes(List<InverterMinuteReading> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return 0;
        }

        var count = batch.Count;
        _minuteRepository.AddMissing(batch.ToList());
        batch.Clear();
        cancellationToken.ThrowIfCancellationRequested();
        return count;
    }

    private int SaveHours(
        List<(SolarPark Park, List<(Inverter Inverter, Guid NodeId)> Inverters)> equipment,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var batch = new List<ParkHourlyGeneration>(HourBatchSize);
        var saved = 0;

        foreach (var (park, inverters) in equipment)
        {
            var firstCreatedAt = inverters.Min(i => i.Inverter.CreatedAtUtc);
            var range = HistoryGapPlanner.MissingHours(_hourRepository.GetLastHourByPark(park.Id), firstCreatedAt, nowUtc);
            if (range is null)
            {
                continue;
            }

            for (var hour = range.Value.FromUtc; hour < range.Value.ToUtc; hour = hour.AddHours(1))
            {
                // En cada hora solo suman los inversores dados de alta hasta su comienzo.
                var simulated = inverters
                    .Where(i => i.Inverter.CreatedAtUtc <= hour)
                    .Select(i => new SimulatedInverter(i.Inverter.Id, i.Inverter.NominalCapacityKw))
                    .ToList();
                var summary = _engine.SummarizeParkHour(park.Id, simulated, hour);
                var local = hour + UruguayOffset;

                batch.Add(new ParkHourlyGeneration
                {
                    Id = Guid.NewGuid(),
                    SolarParkId = park.Id,
                    TimestampUtc = DateTime.SpecifyKind(hour, DateTimeKind.Utc),
                    DateLocal = DateOnly.FromDateTime(local),
                    HourLocal = local.Hour,
                    EnergyGeneratedKwh = summary.EnergyKwh,
                    PeakPowerKw = summary.PeakPowerKw
                });

                if (batch.Count >= HourBatchSize)
                {
                    saved += FlushHours(batch, cancellationToken);
                }
            }
        }

        return saved + FlushHours(batch, cancellationToken);
    }

    private int FlushHours(List<ParkHourlyGeneration> batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return 0;
        }

        var count = batch.Count;
        _hourRepository.AddMissing(batch.ToList());
        batch.Clear();
        cancellationToken.ThrowIfCancellationRequested();
        return count;
    }
}
