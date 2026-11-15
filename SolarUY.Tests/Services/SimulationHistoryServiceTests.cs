using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.ApiService.Simulation;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SimulationHistoryServiceTests
{
    private static readonly Guid ParkId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid NodeA = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid NodeB = Guid.Parse("22222222-0000-0000-0000-000000000002");
    private static readonly Guid InverterA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid InverterB = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    // 15/11/2026 18:00:30 UTC = 15:00:30 en Uruguay.
    private static readonly DateTime Now = new(2026, 11, 15, 18, 0, 30, DateTimeKind.Utc);

    private readonly FakeSolarParkRepository _parks = new();
    private readonly FakeSolarNodeRepository _nodes = new();
    private readonly FakeInverterRepository _inverters = new();
    private readonly FakeInverterMinuteReadingRepository _minutes = new();
    private readonly FakeParkGenerationRepository _hours = new();
    private readonly SolarSimulationEngine _engine = new();
    private readonly FakeHistoryCatchUpLock _lock = new();
    private readonly FakeSimulatedFaultRepository _faults = new();
    private readonly SimulationHistoryService _service;

    public SimulationHistoryServiceTests()
    {
        _service = new SimulationHistoryService(_parks, _nodes, _inverters, _minutes, _hours, _engine, _lock, _faults);
    }

    private static DateTime Utc(int day, int hour, int minute = 0)
        => new(2026, 11, day, hour, minute, 0, DateTimeKind.Utc);

    private void AddPark(SolarParkStatus status = SolarParkStatus.Active)
        => _parks.Parks.Add(new SolarPark { Id = ParkId, Name = "Parque", NormalizedName = "PARQUE", Status = status, MaxCapacityKw = 500 });

    private void AddNode(Guid id, SolarNodeStatus status = SolarNodeStatus.Active)
        => _nodes.PersistedNodes.Add(new SolarNode { Id = id, Name = id.ToString(), NormalizedName = id.ToString(), SolarParkId = ParkId, Status = status });

    private void AddInverter(Guid id, Guid nodeId, double capacityKw, DateTime createdAtUtc)
        => _inverters.Inverters.Add(new Inverter
        {
            Id = id,
            SerieNumber = id.ToString(),
            NormalizedSerieNumber = id.ToString(),
            NominalCapacityKw = capacityKw,
            SolarNodeId = nodeId,
            CreatedAtUtc = createdAtUtc
        });

    private void AddActiveEquipment(DateTime inverterACreatedAtUtc)
    {
        AddPark();
        AddNode(NodeA);
        AddInverter(InverterA, NodeA, 110, inverterACreatedAtUtc);
    }

    // ---------- Minutos ----------

    [Fact]
    public void CatchUp_SavesOneRowPerActiveInverterAndCompleteMinute()
    {
        AddActiveEquipment(Utc(15, 17));
        AddNode(NodeB);
        AddInverter(InverterB, NodeB, 100, Utc(15, 17));

        var result = _service.CatchUp(Now, 30);

        // Del 17:00 al 17:59 (el minuto 18:00 sigue en curso): 60 minutos por inversor.
        Assert.Equal(120, result.MinuteRows);
        Assert.Equal(120, _minutes.Readings.Count);
        Assert.Equal(60, _minutes.Readings.Count(r => r.InverterId == InverterA));
        Assert.Equal(Utc(15, 17), _minutes.Readings.Min(r => r.MinuteUtc));
        Assert.Equal(Utc(15, 17, 59), _minutes.Readings.Max(r => r.MinuteUtc));
    }

    [Fact]
    public void CatchUp_MinuteRowsCarryTheParkAndTheNodeOfTheInverter()
    {
        AddActiveEquipment(Utc(15, 17, 50));

        _service.CatchUp(Now, 30);

        Assert.All(_minutes.Readings, r =>
        {
            Assert.Equal(ParkId, r.SolarParkId);
            Assert.Equal(NodeA, r.SolarNodeId);
            Assert.Equal(InverterA, r.InverterId);
        });
    }

    [Fact]
    public void CatchUp_MinuteRowsAreTheEngineAggregates()
    {
        AddActiveEquipment(Utc(15, 17, 55));

        _service.CatchUp(Now, 30);

        var row = _minutes.Readings.Single(r => r.MinuteUtc == Utc(15, 17, 57));
        var expected = MinuteAggregator.Build(_engine, InverterA, ParkId, NodeA, 110, Utc(15, 17, 57));
        Assert.Equal(expected.EnergyKwh, row.EnergyKwh);
        Assert.Equal(expected.AvgPowerKw, row.AvgPowerKw);
        Assert.Equal(expected.AnomalousSamples, row.AnomalousSamples);
    }

    [Fact]
    public void CatchUp_CalledTwiceWithTheSameInstant_DoesNotDuplicateNorSaveAnything()
    {
        AddActiveEquipment(Utc(15, 12));

        _service.CatchUp(Now, 30);
        var minutes = _minutes.Readings.Count;
        var hours = _hours.Records.Count;

        var second = _service.CatchUp(Now, 30);

        Assert.Equal(0, second.MinuteRows);
        Assert.Equal(0, second.HourRows);
        Assert.Equal(minutes, _minutes.Readings.Count);
        Assert.Equal(hours, _hours.Records.Count);
    }

    [Fact]
    public void CatchUp_AfterSomeMinutesPass_OnlyAddsTheNewMinutes()
    {
        AddActiveEquipment(Utc(15, 17));
        _service.CatchUp(Now, 30);

        var later = _service.CatchUp(Now.AddMinutes(3), 30);

        Assert.Equal(3, later.MinuteRows);
        Assert.Equal(0, later.HourRows);
        Assert.Equal(63, _minutes.Readings.Count);
        Assert.Equal(Utc(15, 18, 2), _minutes.Readings.Max(r => r.MinuteUtc));
    }

    [Fact]
    public void CatchUp_AfterARestartWithAGap_FillsTheGapWithoutTouchingWhatExists()
    {
        AddActiveEquipment(Utc(15, 16));
        _service.CatchUp(Utc(15, 17, 0), 30);
        var firstHour = _minutes.Readings.ToList();

        var result = _service.CatchUp(Now, 30);

        Assert.Equal(60, result.MinuteRows);
        Assert.Equal(120, _minutes.Readings.Count);
        Assert.All(firstHour, row => Assert.Contains(row, _minutes.Readings));
        Assert.Equal(120, _minutes.Readings.Select(r => r.MinuteUtc).Distinct().Count());
    }

    [Fact]
    public void CatchUp_SavesInBatches()
    {
        AddActiveEquipment(Utc(14, 18));
        AddNode(NodeB);
        AddInverter(InverterB, NodeB, 100, Utc(14, 18));

        _service.CatchUp(Now, 30);

        // 2 inversores × 1440 minutos = 2880 filas: no se guardan de una sola vez.
        Assert.Equal(2880, _minutes.Readings.Count);
        Assert.True(_minutes.AddMissingCalls >= 2, $"Llamadas: {_minutes.AddMissingCalls}");
    }

    // ---------- Equipos activos ----------

    [Fact]
    public void CatchUp_SkipsInvertersOfInactiveNodes()
    {
        AddPark();
        AddNode(NodeA, SolarNodeStatus.Inactive);
        AddInverter(InverterA, NodeA, 110, Utc(15, 12));
        AddNode(NodeB);
        AddInverter(InverterB, NodeB, 100, Utc(15, 12));

        _service.CatchUp(Now, 30);

        Assert.DoesNotContain(_minutes.Readings, r => r.InverterId == InverterA);
        Assert.Contains(_minutes.Readings, r => r.InverterId == InverterB);
    }

    [Fact]
    public void CatchUp_SkipsInactiveParksInBothTables()
    {
        AddPark(SolarParkStatus.Inactive);
        AddNode(NodeA);
        AddInverter(InverterA, NodeA, 110, Utc(15, 12));

        var result = _service.CatchUp(Now, 30);

        Assert.Equal(0, result.MinuteRows);
        Assert.Equal(0, result.HourRows);
        Assert.Empty(_minutes.Readings);
        Assert.Empty(_hours.Records);
    }

    [Fact]
    public void CatchUp_ForAParkWithoutInverters_SavesNothing()
    {
        AddPark();
        AddNode(NodeA);

        var result = _service.CatchUp(Now, 30);

        Assert.Equal(0, result.MinuteRows);
        Assert.Equal(0, result.HourRows);
        Assert.Empty(_hours.Records);
    }

    // ---------- Horas ----------

    [Fact]
    public void CatchUp_SavesOneRowPerCompleteHourOfThePark()
    {
        AddActiveEquipment(Utc(15, 12));

        var result = _service.CatchUp(Now, 30);

        // 12:00 a 17:00 UTC = 6 horas completas (09:00 a 14:00 locales).
        Assert.Equal(6, result.HourRows);
        Assert.Equal(6, _hours.Records.Count);
        Assert.Equal(Enumerable.Range(9, 6), _hours.Records.Select(r => r.HourLocal).OrderBy(h => h));
        Assert.All(_hours.Records, r =>
        {
            Assert.Equal(ParkId, r.SolarParkId);
            Assert.Equal(new DateOnly(2026, 11, 15), r.DateLocal);
            Assert.Equal(DateTimeKind.Utc, r.TimestampUtc.Kind);
            Assert.NotEqual(Guid.Empty, r.Id);
        });
    }

    [Fact]
    public void CatchUp_HourRowsMatchTheSumOfTheMinuteRows()
    {
        AddActiveEquipment(Utc(15, 12));
        AddNode(NodeB);
        AddInverter(InverterB, NodeB, 100, Utc(15, 12));

        _service.CatchUp(Now, 30);

        foreach (var hour in _hours.Records)
        {
            var minutes = _minutes.Readings.Where(r => r.MinuteUtc >= hour.TimestampUtc && r.MinuteUtc < hour.TimestampUtc.AddHours(1)).ToList();
            Assert.Equal(120, minutes.Count);
            Assert.Equal(minutes.Sum(m => m.EnergyKwh), hour.EnergyGeneratedKwh, 6);
        }
    }

    [Fact]
    public void CatchUp_HourRowsCarryTheParkPeakAndTheEnergyOfTheEngine()
    {
        AddActiveEquipment(Utc(15, 12));

        _service.CatchUp(Now, 30);

        var row = _hours.Records.Single(r => r.TimestampUtc == Utc(15, 15));
        var expected = _engine.SummarizeParkHour(ParkId, [new SimulatedInverter(InverterA, 110)], Utc(15, 15));
        Assert.Equal(expected.EnergyKwh, row.EnergyGeneratedKwh);
        Assert.Equal(expected.PeakPowerKw, row.PeakPowerKw);
        Assert.True(row.EnergyGeneratedKwh > 0.0);
    }

    [Fact]
    public void CatchUp_NightHoursAreSavedAsZero()
    {
        // 03:00 UTC = 00:00 local: pleno de noche.
        AddActiveEquipment(Utc(15, 3));

        _service.CatchUp(Now, 30);

        var night = _hours.Records.Where(r => r.HourLocal is >= 0 and <= 4).ToList();
        Assert.NotEmpty(night);
        Assert.All(night, r =>
        {
            Assert.Equal(0.0, r.EnergyGeneratedKwh);
            Assert.Equal(0.0, r.PeakPowerKw);
        });
    }

    [Fact]
    public void CatchUp_AnInverterRegisteredMidPeriodOnlyCountsFromItsRegistration()
    {
        AddActiveEquipment(Utc(15, 12));
        AddNode(NodeB);
        AddInverter(InverterB, NodeB, 100, Utc(15, 14));

        _service.CatchUp(Now, 30);

        var onlyA = new[] { new SimulatedInverter(InverterA, 110) };
        var both = new[] { new SimulatedInverter(InverterA, 110), new SimulatedInverter(InverterB, 100) };

        var before = _hours.Records.Single(r => r.TimestampUtc == Utc(15, 13));
        var after = _hours.Records.Single(r => r.TimestampUtc == Utc(15, 14));

        Assert.Equal(_engine.SummarizeParkHour(ParkId, onlyA, Utc(15, 13)).EnergyKwh, before.EnergyGeneratedKwh);
        Assert.Equal(_engine.SummarizeParkHour(ParkId, both, Utc(15, 14)).EnergyKwh, after.EnergyGeneratedKwh);
    }

    [Fact]
    public void CatchUp_WhenTheHourPasses_AddsTheNewHourOnly()
    {
        AddActiveEquipment(Utc(15, 12));
        _service.CatchUp(Now, 30);

        var later = _service.CatchUp(Utc(15, 19, 0).AddSeconds(30), 30);

        Assert.Equal(1, later.HourRows);
        Assert.Equal(7, _hours.Records.Count);
        Assert.Equal(Utc(15, 18), _hours.Records.Max(r => r.TimestampUtc));
    }

    // ---------- Cancelación ----------

    [Fact]
    public void CatchUp_WhenCancelled_StopsAndKeepsWhatWasAlreadySaved()
    {
        AddActiveEquipment(Utc(14, 18));
        AddNode(NodeB);
        AddInverter(InverterB, NodeB, 100, Utc(14, 18));
        using var cts = new CancellationTokenSource();
        _minutes.OnAddMissing = () => cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => _service.CatchUp(Now, 30, cts.Token));

        Assert.Equal(1, _minutes.AddMissingCalls);
        Assert.True(_minutes.Readings.Count > 0 && _minutes.Readings.Count < 2880);
    }

    // ---------- Varias copias del servidor ----------

    [Fact]
    public void CatchUp_WhenAnotherCopyIsCompletingTheHistory_SavesNothingAndDoesNotTouchTheRepositories()
    {
        AddActiveEquipment(Utc(15, 12));
        _lock.Available = false;

        var result = _service.CatchUp(Now, 30);

        Assert.Equal(0, result.MinuteRows);
        Assert.Equal(0, result.HourRows);
        Assert.Empty(_minutes.Readings);
        Assert.Empty(_hours.Records);
        Assert.Equal(0, _lock.Releases);
    }

    [Fact]
    public void CatchUp_ReleasesTheLockWhenItFinishes()
    {
        AddActiveEquipment(Utc(15, 17));

        _service.CatchUp(Now, 30);

        Assert.Equal(1, _lock.Acquisitions);
        Assert.Equal(1, _lock.Releases);
    }

    [Fact]
    public void CatchUp_ReleasesTheLockEvenWhenItIsCancelled()
    {
        AddActiveEquipment(Utc(14, 18));
        using var cts = new CancellationTokenSource();
        _minutes.OnAddMissing = () => cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => _service.CatchUp(Now, 30, cts.Token));

        Assert.Equal(1, _lock.Releases);
    }

    [Fact]
    public void CatchUp_TheNextAttemptAfterTheOtherCopyFinishedCompletesTheGap()
    {
        AddActiveEquipment(Utc(15, 17));
        _lock.Available = false;
        _service.CatchUp(Now, 30);

        _lock.Available = true;
        var result = _service.CatchUp(Now, 30);

        Assert.Equal(60, result.MinuteRows);
    }

    // ---------- Fallas simuladas (spec 037) ----------

    [Fact]
    public void CatchUp_AnInsulationFaultStopsTheInverterFromItsStartInTheMinuteRows()
    {
        AddActiveEquipment(Utc(15, 17));
        _faults.Faults.Add(new SimulatedFault
        {
            Id = Guid.NewGuid(),
            FaultType = SimulatedFaultType.InsulationFault,
            TargetType = SimulatedFaultTarget.Inverter,
            TargetId = InverterA,
            StartedAtUtc = Utc(15, 17, 30)
        });

        _service.CatchUp(Now, 30);

        var before = _minutes.Readings.Where(r => r.MinuteUtc < Utc(15, 17, 30)).ToList();
        var after = _minutes.Readings.Where(r => r.MinuteUtc >= Utc(15, 17, 30)).ToList();
        Assert.Equal(30, before.Count);
        Assert.All(before, r => Assert.True(r.AvgPowerKw > 0.0));
        Assert.Equal(30, after.Count);
        Assert.All(after, r =>
        {
            Assert.Equal(0.0, r.AvgPowerKw);
            Assert.Equal(0.0, r.EnergyKwh);
        });
    }

    [Fact]
    public void CatchUp_AfterTheFaultIsClearedTheMinutesAreNormalAgain()
    {
        AddActiveEquipment(Utc(15, 17));
        _faults.Faults.Add(new SimulatedFault
        {
            Id = Guid.NewGuid(),
            FaultType = SimulatedFaultType.InsulationFault,
            TargetType = SimulatedFaultTarget.Inverter,
            TargetId = InverterA,
            StartedAtUtc = Utc(15, 17, 10),
            EndedAtUtc = Utc(15, 17, 20)
        });

        _service.CatchUp(Now, 30);

        Assert.All(_minutes.Readings.Where(r => r.MinuteUtc >= Utc(15, 17, 10) && r.MinuteUtc < Utc(15, 17, 20)), r => Assert.Equal(0.0, r.AvgPowerKw));
        Assert.All(_minutes.Readings.Where(r => r.MinuteUtc >= Utc(15, 17, 20)), r => Assert.True(r.AvgPowerKw > 0.0));
    }

    [Fact]
    public void CatchUp_TheHourRowReflectsTheFaultAndStillMatchesTheMinuteRows()
    {
        AddActiveEquipment(Utc(15, 12));
        var faultStart = Utc(15, 15, 30);
        _faults.Faults.Add(new SimulatedFault
        {
            Id = Guid.NewGuid(),
            FaultType = SimulatedFaultType.InsulationFault,
            TargetType = SimulatedFaultTarget.Inverter,
            TargetId = InverterA,
            StartedAtUtc = faultStart
        });

        _service.CatchUp(Now, 30);

        var hour = _hours.Records.Single(r => r.TimestampUtc == Utc(15, 15));
        var minutes = _minutes.Readings.Where(r => r.MinuteUtc >= hour.TimestampUtc && r.MinuteUtc < hour.TimestampUtc.AddHours(1)).ToList();
        Assert.Equal(minutes.Sum(m => m.EnergyKwh), hour.EnergyGeneratedKwh, 6);

        var normal = _engine.SummarizeParkHour(ParkId, [new SimulatedInverter(InverterA, 110)], Utc(15, 15));
        Assert.True(hour.EnergyGeneratedKwh < normal.EnergyKwh);
        Assert.True(hour.EnergyGeneratedKwh > 0.0);
    }

    [Fact]
    public void CatchUp_WithoutFaultsTheRowsAreTheSameAsBefore()
    {
        AddActiveEquipment(Utc(15, 17, 55));

        _service.CatchUp(Now, 30);

        var row = _minutes.Readings.Single(r => r.MinuteUtc == Utc(15, 17, 57));
        var expected = MinuteAggregator.Build(_engine, InverterA, ParkId, NodeA, 110, Utc(15, 17, 57));
        Assert.Equal(expected.AvgPowerKw, row.AvgPowerKw);
    }
}
