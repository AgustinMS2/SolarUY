using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class SolarSimulationEngineFaultTests
{
    private static readonly Guid Park = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Node = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid InverterA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid InverterB = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid PanelA = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid PanelB = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    private readonly SolarSimulationEngine _engine = new();

    /// <summary>Instante UTC que corresponde a una fecha y hora locales de Uruguay (UTC−3).</summary>
    private static DateTime Utc(int month, int day, double localHour)
        => new DateTime(2026, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(localHour + 3.0);

    private static DateTime Noon => Utc(11, 15, 12.75);

    private static FaultTimeline Fault(Guid target, SimulatedFaultType type, DateTime start, DateTime? end = null)
        => new([new FaultWindow(target, type, start, end)]);

    private static IReadOnlyCollection<SimulatedInverter> Both => [new(InverterA, 110), new(InverterB, 100)];

    // ---------- Sin fallas: igual que antes ----------

    [Fact]
    public void WithoutFaults_EveryReadingIsIdenticalToTheOneWithAnEmptyTimeline()
    {
        var instant = Noon.AddMinutes(7);

        Assert.Equal(_engine.ReadInverter(InverterA, Park, 110, instant), _engine.ReadInverter(InverterA, Park, 110, instant, FaultTimeline.Empty));
        Assert.Equal(_engine.ReadPanelPowerW(PanelA, Park, 400, instant), _engine.ReadPanelPowerW(PanelA, Park, 400, instant, FaultTimeline.Empty));
        Assert.Equal(_engine.ReadParkPowerKw(Park, Both, instant), _engine.ReadParkPowerKw(Park, Both, instant, FaultTimeline.Empty));
    }

    [Fact]
    public void AFaultOnAnotherEquipment_DoesNotChangeAReading()
    {
        var timeline = Fault(PanelB, SimulatedFaultType.OpenCircuit, Noon.AddHours(-1));

        Assert.Equal(_engine.ReadPanelPowerW(PanelA, Park, 400, Noon), _engine.ReadPanelPowerW(PanelA, Park, 400, Noon, timeline));
    }

    // ---------- Panel: corte total ----------

    [Fact]
    public void OpenCircuit_PanelPowerDropsToZeroInFullDaylight()
    {
        var timeline = Fault(PanelA, SimulatedFaultType.OpenCircuit, Noon.AddMinutes(-5));

        Assert.True(_engine.ReadPanelPowerW(PanelA, Park, 400, Noon) > 300.0);
        Assert.Equal(0.0, _engine.ReadPanelPowerW(PanelA, Park, 400, Noon, timeline));
    }

    [Fact]
    public void OpenCircuit_TheChangeIsInstantaneousAtTheStartAndEndsAtTheEnd()
    {
        var start = Noon;
        var end = Noon.AddMinutes(30);
        var timeline = Fault(PanelA, SimulatedFaultType.OpenCircuit, start, end);

        var justBefore = start.AddSeconds(-1);
        Assert.Equal(_engine.ReadPanelPowerW(PanelA, Park, 400, justBefore), _engine.ReadPanelPowerW(PanelA, Park, 400, justBefore, timeline));
        Assert.Equal(0.0, _engine.ReadPanelPowerW(PanelA, Park, 400, start, timeline));
        Assert.Equal(0.0, _engine.ReadPanelPowerW(PanelA, Park, 400, end.AddSeconds(-1), timeline));
        Assert.Equal(_engine.ReadPanelPowerW(PanelA, Park, 400, end), _engine.ReadPanelPowerW(PanelA, Park, 400, end, timeline));
    }

    // ---------- Panel: degradación severa ----------

    [Fact]
    public void SevereDegradation_PanelDeliversAboutThirtyPercentOfWhatIsExpected()
    {
        var timeline = Fault(PanelA, SimulatedFaultType.SevereDegradation, Utc(11, 15, 0));

        var checkedSamples = 0;
        for (var minutes = 0; minutes < 9 * 60; minutes += 7)
        {
            var instant = Utc(11, 15, 8.0).AddMinutes(minutes);
            var expected = 400.0 * SolarCalendar.Factor(SolarCalendar.ToLocal(instant));
            if (expected < 40.0)
            {
                continue;
            }

            var ratio = _engine.ReadPanelPowerW(PanelA, Park, 400, instant, timeline) / expected;
            Assert.InRange(ratio, 0.27, 0.33);
            checkedSamples++;
        }

        Assert.True(checkedSamples > 40);
    }

    [Fact]
    public void SevereDegradation_NeverFallsToTheCriticalLevelOfTheHealthMap()
    {
        // El estado "Falla Crítica" es menos del 10 % de lo esperado; la degradación debe verse siempre ámbar.
        var timeline = Fault(PanelA, SimulatedFaultType.SevereDegradation, Utc(11, 1, 0));

        for (var minutes = 0; minutes < 14 * 60; minutes += 3)
        {
            var instant = Utc(11, 15, 6.5).AddMinutes(minutes);
            var expected = 400.0 * SolarCalendar.Factor(SolarCalendar.ToLocal(instant));
            if (expected < 20.0)
            {
                continue;
            }

            Assert.True(_engine.ReadPanelPowerW(PanelA, Park, 400, instant, timeline) >= 0.10 * expected);
        }
    }

    [Fact]
    public void SevereDegradation_AtNightThePowerStaysZero()
    {
        var timeline = Fault(PanelA, SimulatedFaultType.SevereDegradation, Utc(11, 15, 0));

        Assert.Equal(0.0, _engine.ReadPanelPowerW(PanelA, Park, 400, Utc(11, 15, 2.0), timeline));
    }

    [Fact]
    public void TheCriticalPanelFaultPrevailsOverTheDegradation()
    {
        var timeline = new FaultTimeline([
            new FaultWindow(PanelA, SimulatedFaultType.SevereDegradation, Noon.AddHours(-1), null),
            new FaultWindow(PanelA, SimulatedFaultType.OpenCircuit, Noon.AddMinutes(-10), null)]);

        Assert.Equal(0.0, _engine.ReadPanelPowerW(PanelA, Park, 400, Noon, timeline));
    }

    // ---------- Inversor: sobretemperatura ----------

    [Fact]
    public void OverTemperature_TheInverterTemperatureIsBetween86And90DegreesDayAndNight()
    {
        var timeline = Fault(InverterA, SimulatedFaultType.InverterOverTemperature, Utc(11, 1, 0));

        for (var minutes = 0; minutes < 24 * 60; minutes += 11)
        {
            var temperature = _engine.ReadInverter(InverterA, Park, 110, Utc(11, 15, 0).AddMinutes(minutes), timeline).TemperatureC;
            Assert.InRange(temperature, 86.0, 90.0);
        }
    }

    [Fact]
    public void OverTemperature_DoesNotChangeThePowerNorTheElectricalValues()
    {
        var timeline = Fault(InverterA, SimulatedFaultType.InverterOverTemperature, Utc(11, 1, 0));

        var normal = _engine.ReadInverter(InverterA, Park, 110, Noon);
        var faulty = _engine.ReadInverter(InverterA, Park, 110, Noon, timeline);

        Assert.Equal(normal with { TemperatureC = 0 }, faulty with { TemperatureC = 0 });
        Assert.True(faulty.TemperatureC > normal.TemperatureC);
    }

    // ---------- Inversor: falla de aislamiento ----------

    [Fact]
    public void InsulationFault_TheInverterShutsDownInFullDaylight()
    {
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, Noon.AddMinutes(-5));

        var normal = _engine.ReadInverter(InverterA, Park, 110, Noon);
        var faulty = _engine.ReadInverter(InverterA, Park, 110, Noon, timeline);

        Assert.True(normal.PowerKw > 80.0);
        Assert.Equal(0.0, faulty.PowerKw);
        Assert.Equal(0.0, faulty.DcCurrentA);
        Assert.Equal(0.0, faulty.AcVoltageV);
        Assert.Equal(0.0, faulty.FrequencyHz);
        Assert.InRange(faulty.DcVoltageV, 4.0, 12.0);
        Assert.True(faulty.TemperatureC < 40.0, "Apagado, el inversor se enfría hasta la temperatura ambiente.");
    }

    [Fact]
    public void InsulationFault_TheCriticalInverterFaultPrevailsOverTheOverTemperature()
    {
        var timeline = new FaultTimeline([
            new FaultWindow(InverterA, SimulatedFaultType.InverterOverTemperature, Noon.AddHours(-1), null),
            new FaultWindow(InverterA, SimulatedFaultType.InsulationFault, Noon.AddMinutes(-10), null)]);

        var reading = _engine.ReadInverter(InverterA, Park, 110, Noon, timeline);

        Assert.Equal(0.0, reading.PowerKw);
        Assert.True(reading.TemperatureC < 40.0);
    }

    [Fact]
    public void InsulationFault_TheParkPowerIsTheSumOfTheInvertersThatStillWork()
    {
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, Noon.AddMinutes(-5));

        var onlyB = _engine.ReadParkPowerKw(Park, [new SimulatedInverter(InverterB, 100)], Noon);

        Assert.Equal(onlyB, _engine.ReadParkPowerKw(Park, Both, Noon, timeline), 9);
    }

    [Fact]
    public void InsulationFault_AMinuteAfterTheStartGeneratesNothing()
    {
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, Noon.AddMinutes(-30));
        var minute = new DateTime(Noon.Ticks - (Noon.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);

        Assert.True(_engine.InverterMinuteEnergyKwh(InverterA, Park, 110, minute) > 0.5);
        Assert.Equal(0.0, _engine.InverterMinuteEnergyKwh(InverterA, Park, 110, minute, timeline));
    }

    // ---------- Energía del día y resumen horario ----------

    [Fact]
    public void EnergyToday_CountsTheFaultOnlyFromItsStart()
    {
        var start = Utc(11, 15, 12.0);
        var now = Utc(11, 15, 14.0);
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, start);

        var expected = _engine.EnergyTodayKwh(Park, [new SimulatedInverter(InverterA, 110)], start)
                       + _engine.EnergyTodayKwh(Park, [new SimulatedInverter(InverterB, 100)], now);

        Assert.Equal(expected, _engine.EnergyTodayKwh(Park, Both, now, timeline), 6);
    }

    [Fact]
    public void HourSummary_AFaultStartingMidHourGivesAnIntermediateEnergy()
    {
        var hour = Utc(11, 15, 12.0);
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, hour.AddMinutes(30));

        var normal = _engine.SummarizeParkHour(Park, Both, hour).EnergyKwh;
        var withFault = _engine.SummarizeParkHour(Park, Both, hour, timeline).EnergyKwh;
        var onlyB = _engine.SummarizeParkHour(Park, [new SimulatedInverter(InverterB, 100)], hour).EnergyKwh;

        Assert.True(withFault < normal);
        Assert.True(withFault > onlyB);
    }

    [Fact]
    public void HourSummary_IsTheSumOfTheMinuteEnergiesAlsoWithAFault()
    {
        var hour = Utc(11, 15, 12.0);
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, hour.AddMinutes(17).AddSeconds(30));

        var expected = 0.0;
        for (var minute = 0; minute < 60; minute++)
        {
            foreach (var inverter in Both)
            {
                expected += _engine.InverterMinuteEnergyKwh(inverter.InverterId, Park, inverter.NominalCapacityKw, hour.AddMinutes(minute), timeline);
            }
        }

        Assert.Equal(expected, _engine.SummarizeParkHour(Park, Both, hour, timeline).EnergyKwh, 9);
    }

    // ---------- Resumen por minuto ----------

    [Fact]
    public void MinuteSummary_WithInsulationFault_HasNoPowerAndCountsEverySampleAsAnomalous()
    {
        var minute = new DateTime(Noon.Ticks - (Noon.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);
        var timeline = Fault(InverterA, SimulatedFaultType.InsulationFault, minute.AddMinutes(-10));

        var row = MinuteAggregator.Build(_engine, InverterA, Park, Node, 110, minute, timeline);

        Assert.Equal(0.0, row.AvgPowerKw);
        Assert.Equal(0.0, row.EnergyKwh);
        Assert.Equal(6, row.AnomalousSamples);
    }

    [Fact]
    public void MinuteSummary_WithOverTemperature_CountsEverySampleAsAnomalousAndKeepsThePower()
    {
        var minute = new DateTime(Noon.Ticks - (Noon.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);
        var timeline = Fault(InverterA, SimulatedFaultType.InverterOverTemperature, minute.AddMinutes(-10));

        var normal = MinuteAggregator.Build(_engine, InverterA, Park, Node, 110, minute);
        var faulty = MinuteAggregator.Build(_engine, InverterA, Park, Node, 110, minute, timeline);

        Assert.Equal(normal.AvgPowerKw, faulty.AvgPowerKw);
        Assert.InRange(faulty.AvgTemperatureC, 86.0, 90.0);
        Assert.Equal(6, faulty.AnomalousSamples);
    }

    [Fact]
    public void MinuteSummary_WithoutATimeline_IsTheSameAsBefore()
    {
        var minute = new DateTime(Noon.Ticks - (Noon.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc);

        var a = MinuteAggregator.Build(_engine, InverterA, Park, Node, 110, minute);
        var b = MinuteAggregator.Build(_engine, InverterA, Park, Node, 110, minute, FaultTimeline.Empty);

        Assert.Equal(a.AvgPowerKw, b.AvgPowerKw);
        Assert.Equal(a.AvgTemperatureC, b.AvgTemperatureC);
        Assert.Equal(a.AnomalousSamples, b.AnomalousSamples);
    }
}
