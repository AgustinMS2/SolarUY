using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class MinuteAggregatorTests
{
    private static readonly Guid Park = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Node = Guid.Parse("22222222-0000-0000-0000-000000000001");
    private static readonly Guid Inverter = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly SolarSimulationEngine _engine = new();

    private static DateTime Utc(int year, int month, int day, double localHour)
        => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(localHour + 3.0);

    private InverterMinuteReading Build(DateTime minute)
        => MinuteAggregator.Build(_engine, Inverter, Park, Node, 110, minute);

    [Fact]
    public void Build_CopiesTheIdentityOfTheEquipmentAndTheMinute()
    {
        var minute = Utc(2026, 11, 15, 12.0);

        var row = Build(minute);

        Assert.Equal(Inverter, row.InverterId);
        Assert.Equal(Park, row.SolarParkId);
        Assert.Equal(Node, row.SolarNodeId);
        Assert.Equal(minute, row.MinuteUtc);
        Assert.Equal(DateTimeKind.Utc, row.MinuteUtc.Kind);
    }

    [Fact]
    public void Build_SummarizesSixSamplesOfTenSeconds()
    {
        Assert.Equal(6, Build(Utc(2026, 11, 15, 12.0)).SampleCount);
    }

    [Fact]
    public void Build_EnergyIsTheEngineEnergyOfTheMinute()
    {
        var minute = Utc(2026, 11, 15, 12.0);

        var row = Build(minute);

        Assert.Equal(_engine.InverterMinuteEnergyKwh(Inverter, Park, 110, minute), row.EnergyKwh, 12);
    }

    [Fact]
    public void Build_EnergyIsTheAveragePowerOverOneMinute()
    {
        var row = Build(Utc(2026, 11, 15, 12.0));

        Assert.Equal(row.AvgPowerKw / 60.0, row.EnergyKwh, 9);
        Assert.True(row.EnergyKwh > 0.0);
    }

    [Fact]
    public void Build_AveragesAndMaximumsComeFromTheSixSamples()
    {
        var minute = Utc(2026, 11, 15, 12.0);
        var samples = Enumerable.Range(0, 6)
            .Select(i => _engine.ReadInverter(Inverter, Park, 110, minute.AddSeconds(i * 10)))
            .ToList();

        var row = Build(minute);

        Assert.Equal(samples.Average(s => s.PowerKw), row.AvgPowerKw, 12);
        Assert.Equal(samples.Max(s => s.PowerKw), row.MaxPowerKw, 12);
        Assert.Equal(samples.Average(s => s.DcVoltageV), row.AvgDcVoltageV, 12);
        Assert.Equal(samples.Average(s => s.DcCurrentA), row.AvgDcCurrentA, 12);
        Assert.Equal(samples.Average(s => s.AcVoltageV), row.AvgAcVoltageV, 12);
        Assert.Equal(samples.Average(s => s.FrequencyHz), row.AvgFrequencyHz, 12);
        Assert.Equal(samples.Average(s => s.TemperatureC), row.AvgTemperatureC, 12);
        Assert.Equal(samples.Max(s => s.TemperatureC), row.MaxTemperatureC, 12);
        Assert.True(row.MaxPowerKw >= row.AvgPowerKw);
        Assert.True(row.MaxTemperatureC >= row.AvgTemperatureC);
    }

    [Fact]
    public void Build_AtNight_HasNoPowerNorEnergyButStillKeepsTheRecord()
    {
        var row = Build(Utc(2026, 11, 15, 2.0));

        Assert.Equal(0.0, row.AvgPowerKw);
        Assert.Equal(0.0, row.MaxPowerKw);
        Assert.Equal(0.0, row.EnergyKwh);
        Assert.Equal(0.0, row.AvgDcCurrentA);
        Assert.Equal(6, row.SampleCount);
        Assert.True(row.AvgAcVoltageV > 300.0);
    }

    [Fact]
    public void Build_IsDeterministic()
    {
        var minute = Utc(2026, 11, 15, 9.0);

        var a = Build(minute);
        var b = Build(minute);

        Assert.Equal(a.AvgPowerKw, b.AvgPowerKw);
        Assert.Equal(a.EnergyKwh, b.EnergyKwh);
        Assert.Equal(a.AvgFrequencyHz, b.AvgFrequencyHz);
        Assert.Equal(a.AnomalousSamples, b.AnomalousSamples);
    }

    [Fact]
    public void Build_CountsAnomalousSamples_OnlyWhenFrequencyOrTemperatureAreOutOfRange()
    {
        var withAnomaly = 0;
        var withoutAnomaly = 0;
        var start = Utc(2026, 11, 15, 8.0);

        for (var i = 0; i < 1500; i++)
        {
            var row = Build(start.AddMinutes(i));
            var outOfRange = Math.Abs(row.AvgFrequencyHz - 50.0) > 0.5;

            if (row.AnomalousSamples > 0)
            {
                withAnomaly++;
            }
            else
            {
                withoutAnomaly++;
                Assert.False(outOfRange, "Un minuto con frecuencia fuera de rango debe contar sus muestras anómalas.");
            }

            Assert.InRange(row.AnomalousSamples, 0, 6);
        }

        Assert.True(withAnomaly > 0, "En 1500 minutos debe aparecer al menos una anomalía de frecuencia.");
        Assert.True(withoutAnomaly > withAnomaly, "Las anomalías deben ser la excepción.");
    }

    [Fact]
    public void Build_WhenTheMinuteDoesNotStartOnAWholeMinute_Throws()
    {
        Assert.Throws<ArgumentException>(() => MinuteAggregator.Build(_engine, Inverter, Park, Node, 110, Utc(2026, 11, 15, 12.0).AddSeconds(10)));
    }
}
