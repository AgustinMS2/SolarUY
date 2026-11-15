using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class SolarSimulationEngineTests
{
    private static readonly Guid ParkSalto = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid ParkCerro = Guid.Parse("11111111-0000-0000-0000-000000000002");
    private static readonly Guid InverterA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid InverterB = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    private static readonly Guid PanelA = Guid.Parse("cccccccc-0000-0000-0000-000000000001");

    private readonly SolarSimulationEngine _engine = new();

    /// <summary>Instante UTC que corresponde a una fecha y hora locales de Uruguay (UTC−3).</summary>
    private static DateTime Utc(int year, int month, int day, double localHour)
        => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(localHour + 3.0);

    private InverterReading Read(Guid inverterId, Guid parkId, double capacity, DateTime utc)
        => _engine.ReadInverter(inverterId, parkId, capacity, utc);

    // ---------- Determinismo ----------

    [Fact]
    public void ReadInverter_ForTheSameEquipmentAndInstant_AlwaysGivesTheSameReading()
    {
        var instant = Utc(2026, 11, 15, 11.3);

        Assert.Equal(Read(InverterA, ParkSalto, 110, instant), Read(InverterA, ParkSalto, 110, instant));
    }

    [Fact]
    public void ReadInverter_DoesNotDependOnTheDateKind()
    {
        var utc = Utc(2026, 11, 15, 11.3);
        var unspecified = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);

        Assert.Equal(Read(InverterA, ParkSalto, 110, utc), Read(InverterA, ParkSalto, 110, unspecified));
    }

    [Fact]
    public void ReadInverter_TwoInstancesOfTheEngineGiveTheSameNumbers()
    {
        var other = new SolarSimulationEngine();
        var instant = Utc(2026, 11, 15, 14.0);

        Assert.Equal(_engine.ReadInverter(InverterA, ParkSalto, 110, instant), other.ReadInverter(InverterA, ParkSalto, 110, instant));
        Assert.Equal(_engine.ReadPanelPowerW(PanelA, ParkSalto, 400, instant), other.ReadPanelPowerW(PanelA, ParkSalto, 400, instant));
    }

    [Fact]
    public void ReadInverter_DifferentInvertersDifferAtTheSameInstants()
    {
        var different = 0;
        for (var i = 0; i < 50; i++)
        {
            var instant = Utc(2026, 11, 15, 11.0).AddMinutes(i);
            if (Math.Abs(Read(InverterA, ParkSalto, 100, instant).PowerKw - Read(InverterB, ParkSalto, 100, instant).PowerKw) > 1e-6)
            {
                different++;
            }
        }

        Assert.True(different > 40);
    }

    // ---------- Noche ----------

    [Theory]
    [InlineData(0.5)]
    [InlineData(2.0)]
    [InlineData(4.0)]
    [InlineData(22.0)]
    public void ReadInverter_ByNight_HasNoPowerAndOnlyAResidualDcVoltage(double localHour)
    {
        var reading = Read(InverterA, ParkSalto, 110, Utc(2026, 11, 15, localHour));

        Assert.Equal(0.0, reading.PowerKw);
        Assert.Equal(0.0, reading.DcCurrentA);
        Assert.InRange(reading.DcVoltageV, 0.0, 30.0);
        Assert.InRange(reading.TemperatureC, 5.0, 25.0);
        Assert.InRange(reading.AcVoltageV, 370.0, 390.0);
    }

    // ---------- Día ----------

    [Fact]
    public void ReadInverter_AroundSolarNoon_StaysWithinTheCapacityAndGetsCloseToIt()
    {
        const double capacity = 110.0;
        var start = Utc(2026, 11, 15, 12.0);
        double sum = 0;
        var near = 0;
        const int samples = 600;

        for (var i = 0; i < samples; i++)
        {
            var power = Read(InverterA, ParkSalto, capacity, start.AddSeconds(i * 10)).PowerKw;
            Assert.InRange(power, 0.0, capacity);
            sum += power;
            if (power > 0.95 * capacity)
            {
                near++;
            }
        }

        Assert.True(sum / samples >= 0.85 * capacity, $"Promedio {sum / samples}");
        Assert.True(near > 0);
    }

    [Fact]
    public void ReadInverter_ByDay_DcVoltageIsInsideTheOperatingRangeAndTheCurrentMatchesThePower()
    {
        for (var i = 0; i < 300; i++)
        {
            var reading = Read(InverterA, ParkSalto, 110, Utc(2026, 11, 15, 8.0).AddMinutes(i));

            Assert.InRange(reading.DcVoltageV, 200.0, 1000.0);
            Assert.Equal(reading.PowerKw * 1000.0 / reading.DcVoltageV, reading.DcCurrentA, precision: 6);
        }
    }

    [Fact]
    public void ReadInverter_AcVoltageAndFrequencyStayInTheirNormalRanges_AndFrequencyAnomaliesAreOccasional()
    {
        var anomalies = 0;
        const int samples = 6000;

        for (var i = 0; i < samples; i++)
        {
            var reading = Read(InverterA, ParkSalto, 110, Utc(2026, 11, 15, 0.0).AddMinutes(i * 3));

            Assert.InRange(reading.AcVoltageV, 370.0, 390.0);
            Assert.InRange(reading.FrequencyHz, 49.0, 51.0);
            if (Math.Abs(reading.FrequencyHz - 50.0) > 0.5)
            {
                anomalies++;
            }
        }

        Assert.InRange(anomalies / (double)samples, 0.01, 0.10);
    }

    [Fact]
    public void ReadInverter_AtSummerNoon_TheTemperatureIsPlausibleAndSometimesExceedsSeventyFiveDegrees()
    {
        var hot = 0;
        const int samples = 2000;
        double max = 0, min = double.MaxValue;

        for (var i = 0; i < samples; i++)
        {
            var reading = Read(InverterA, ParkSalto, 110, Utc(2026, 12, 21, 12.4).AddSeconds(i * 3));
            max = Math.Max(max, reading.TemperatureC);
            min = Math.Min(min, reading.TemperatureC);
            if (reading.TemperatureC > 75.0)
            {
                hot++;
            }
        }

        Assert.True(max < 85.0, $"Máxima {max}");
        Assert.True(min > 55.0, $"Mínima {min}");
        Assert.InRange(hot / (double)samples, 0.03, 0.85);
    }

    // ---------- Nubosidad ----------

    [Fact]
    public void ReadInverter_InvertersOfTheSamePark_ShareTheSameClouds()
    {
        // A las 09:00 la curva es ~0,6: la potencia no llega al tope y la nubosidad se nota por completo.
        for (var i = 0; i < 100; i++)
        {
            var instant = Utc(2026, 11, 15, 9.0).AddMinutes(i);
            var curve = SolarCalendar.Factor(instant.AddHours(-3));
            var ratioA = Read(InverterA, ParkSalto, 100, instant).PowerKw / (100 * curve);
            var ratioB = Read(InverterB, ParkSalto, 100, instant).PowerKw / (100 * curve);

            Assert.True(Math.Abs(ratioA - ratioB) <= 0.045, $"Diferencia {Math.Abs(ratioA - ratioB)}");
        }
    }

    [Fact]
    public void ReadInverter_InvertersOfDifferentParks_DoNotShareTheClouds()
    {
        var differ = 0;
        for (var i = 0; i < 200; i++)
        {
            var instant = Utc(2026, 11, 15, 9.0).AddMinutes(i);
            var curve = SolarCalendar.Factor(instant.AddHours(-3));
            var ratioSalto = Read(InverterA, ParkSalto, 100, instant).PowerKw / (100 * curve);
            var ratioCerro = Read(InverterA, ParkCerro, 100, instant).PowerKw / (100 * curve);
            if (Math.Abs(ratioSalto - ratioCerro) > 0.05)
            {
                differ++;
            }
        }

        Assert.True(differ > 0);
    }

    [Fact]
    public void ReadInverter_CloudVariationStaysWithinTenPercentOfTheCurve()
    {
        for (var i = 0; i < 500; i++)
        {
            var instant = Utc(2026, 11, 15, 9.0).AddMinutes(i / 2.0);
            var curve = SolarCalendar.Factor(instant.AddHours(-3));
            var ratio = Read(InverterA, ParkSalto, 100, instant).PowerKw / (100 * curve);

            Assert.InRange(ratio, 0.88, 1.12);
        }
    }

    // ---------- Paneles ----------

    [Fact]
    public void ReadPanelPowerW_ByNight_IsZero()
    {
        Assert.Equal(0.0, _engine.ReadPanelPowerW(PanelA, ParkSalto, 400, Utc(2026, 11, 15, 1.0)));
    }

    [Fact]
    public void ReadPanelPowerW_ByDay_NeverExceedsTheNameplatePower()
    {
        for (var i = 0; i < 400; i++)
        {
            var power = _engine.ReadPanelPowerW(PanelA, ParkSalto, 400, Utc(2026, 11, 15, 10.0).AddMinutes(i));
            Assert.InRange(power, 0.0, 400.0);
        }
    }

    [Fact]
    public void ReadPanelPowerW_HasOccasionalShadingEventsThatDropItBelowFortyPercentOfExpected()
    {
        var shaded = 0;
        const int blocks = 600;
        for (var i = 0; i < blocks; i++)
        {
            var instant = Utc(2026, 11, 15, 12.0).AddDays(i / 10).AddMinutes((i % 10) * 10);
            var expected = 400 * SolarCalendar.Factor(instant.AddHours(-3));
            var power = _engine.ReadPanelPowerW(PanelA, ParkSalto, 400, instant);
            if (power < 0.4 * expected)
            {
                shaded++;
            }
        }

        Assert.InRange(shaded / (double)blocks, 0.005, 0.10);
    }

    // ---------- Parque ----------

    [Fact]
    public void ReadParkPowerKw_IsTheSumOfItsInverters()
    {
        var inverters = new[] { new SimulatedInverter(InverterA, 110), new SimulatedInverter(InverterB, 100) };
        var instant = Utc(2026, 11, 15, 12.5);

        var expected = Read(InverterA, ParkSalto, 110, instant).PowerKw + Read(InverterB, ParkSalto, 100, instant).PowerKw;

        Assert.Equal(expected, _engine.ReadParkPowerKw(ParkSalto, inverters, instant), precision: 9);
    }

    [Fact]
    public void ReadParkPowerKw_WithoutInverters_IsZero()
    {
        Assert.Equal(0.0, _engine.ReadParkPowerKw(ParkSalto, Array.Empty<SimulatedInverter>(), Utc(2026, 11, 15, 12.5)));
    }

    [Fact]
    public void ReadParkPowerKw_ByNight_IsZero()
    {
        var inverters = new[] { new SimulatedInverter(InverterA, 110) };

        Assert.Equal(0.0, _engine.ReadParkPowerKw(ParkSalto, inverters, Utc(2026, 11, 15, 2.0)));
    }

    // ---------- Energía ----------

    [Fact]
    public void InverterMinuteEnergyKwh_IsTheMeanPowerOfTheSixSamplesDividedBySixty()
    {
        var minute = Utc(2026, 11, 15, 12.0);
        double mean = 0;
        for (var i = 0; i < SolarSimulationEngine.SamplesPerMinute; i++)
        {
            mean += Read(InverterA, ParkSalto, 110, minute.AddSeconds(i * SolarSimulationEngine.SampleSeconds)).PowerKw;
        }

        mean /= SolarSimulationEngine.SamplesPerMinute;

        Assert.Equal(mean / 60.0, _engine.InverterMinuteEnergyKwh(InverterA, ParkSalto, 110, minute), precision: 9);
    }

    [Fact]
    public void EnergyTodayKwh_JustAfterMidnight_IsAlmostZero()
    {
        var inverters = new[] { new SimulatedInverter(InverterA, 110) };

        Assert.True(_engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 11, 15, 0.5)) < 0.01);
    }

    [Fact]
    public void EnergyTodayKwh_GrowsDuringTheDayAndStopsGrowingAtNight()
    {
        var inverters = new[] { new SimulatedInverter(InverterA, 110), new SimulatedInverter(InverterB, 100) };
        var hours = new[] { 6.0, 9.0, 12.0, 15.0, 18.0, 21.0, 23.5 };
        var previous = -1.0;

        foreach (var hour in hours)
        {
            var energy = _engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 11, 15, hour));
            Assert.True(energy >= previous, $"La energía bajó a las {hour}");
            previous = energy;
        }

        Assert.Equal(
            _engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 11, 15, 21.0)),
            _engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 11, 15, 23.5)),
            precision: 6);
    }

    [Fact]
    public void EnergyTodayKwh_IsTheSumOfTheMinuteEnergiesSinceMidnight()
    {
        var inverters = new[] { new SimulatedInverter(InverterA, 110), new SimulatedInverter(InverterB, 100) };
        var midnight = Utc(2026, 11, 15, 0.0);
        var noon = Utc(2026, 11, 15, 12.0);

        double sum = 0;
        for (var minute = midnight; minute < noon; minute = minute.AddMinutes(1))
        {
            foreach (var inverter in inverters)
            {
                sum += _engine.InverterMinuteEnergyKwh(inverter.InverterId, ParkSalto, inverter.NominalCapacityKw, minute);
            }
        }

        Assert.Equal(sum, _engine.EnergyTodayKwh(ParkSalto, inverters, noon), precision: 6);
    }

    [Fact]
    public void EnergyTodayKwh_WithoutInverters_IsZero()
    {
        Assert.Equal(0.0, _engine.EnergyTodayKwh(ParkSalto, Array.Empty<SimulatedInverter>(), Utc(2026, 11, 15, 15.0)));
    }

    [Fact]
    public void EnergyTodayKwh_ADayOfSunEndsWithAPlausibleTotal()
    {
        // 110 kW durante ~13,9 h de sol con la curva sinusoidal: ≈ 110 × 13,9 × 2/π ≈ 970 kWh, menos la nubosidad.
        var inverters = new[] { new SimulatedInverter(InverterA, 110) };

        var total = _engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 11, 15, 23.9));

        Assert.InRange(total, 800.0, 1100.0);
    }

    [Fact]
    public void EnergyTodayKwh_IsShorterInWinterThanInSummer()
    {
        var inverters = new[] { new SimulatedInverter(InverterA, 110) };

        var winter = _engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 6, 21, 23.9));
        var summer = _engine.EnergyTodayKwh(ParkSalto, inverters, Utc(2026, 12, 21, 23.9));

        Assert.True(summer > winter * 1.25, $"Verano {summer}, invierno {winter}");
    }
}
