using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class ParkHourSummaryTests
{
    private static readonly Guid Park = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid InverterA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid InverterB = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");

    private static readonly SimulatedInverter[] Inverters =
    [
        new(InverterA, 110),
        new(InverterB, 100)
    ];

    private readonly SolarSimulationEngine _engine = new();

    private static DateTime Utc(int year, int month, int day, int localHour)
        => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(localHour + 3);

    [Theory]
    [InlineData(2026, 11, 15, 12)]
    [InlineData(2026, 11, 15, 5)]    // el amanecer cae dentro de la hora
    [InlineData(2026, 11, 15, 19)]   // el atardecer cae dentro de la hora
    [InlineData(2026, 6, 21, 7)]
    [InlineData(2026, 12, 21, 17)]
    public void Summarize_EnergyIsTheSumOfTheMinuteEnergiesOfItsInverters(int year, int month, int day, int hour)
    {
        var hourStart = Utc(year, month, day, hour);

        var expected = 0.0;
        for (var minute = 0; minute < 60; minute++)
        {
            foreach (var inverter in Inverters)
            {
                expected += _engine.InverterMinuteEnergyKwh(inverter.InverterId, Park, inverter.NominalCapacityKw, hourStart.AddMinutes(minute));
            }
        }

        var summary = _engine.SummarizeParkHour(Park, Inverters, hourStart);

        Assert.Equal(expected, summary.EnergyKwh, 9);
    }

    [Fact]
    public void Summarize_AtNoon_HasPositiveEnergyAndAPeakAboveTheAveragePower()
    {
        var summary = _engine.SummarizeParkHour(Park, Inverters, Utc(2026, 11, 15, 12));

        Assert.True(summary.EnergyKwh > 100.0);
        Assert.True(summary.PeakPowerKw >= summary.EnergyKwh, "El pico no puede ser menor que la potencia media de la hora (kWh en una hora = kW medios).");
        Assert.True(summary.PeakPowerKw <= 210.0, "El pico no puede superar la capacidad sumada de los inversores.");
    }

    [Fact]
    public void Summarize_PeakIsTheMaximumParkPowerAmongTheTenSecondSamples()
    {
        var hourStart = Utc(2026, 11, 15, 11);

        var expectedPeak = Enumerable.Range(0, 360)
            .Max(i => _engine.ReadParkPowerKw(Park, Inverters, hourStart.AddSeconds(i * 10)));

        Assert.Equal(expectedPeak, _engine.SummarizeParkHour(Park, Inverters, hourStart).PeakPowerKw, 9);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(23)]
    public void Summarize_ForAHourOfNight_IsZero(int hour)
    {
        var summary = _engine.SummarizeParkHour(Park, Inverters, Utc(2026, 11, 15, hour));

        Assert.Equal(0.0, summary.EnergyKwh);
        Assert.Equal(0.0, summary.PeakPowerKw);
    }

    [Fact]
    public void Summarize_WithoutInverters_IsZero()
    {
        var summary = _engine.SummarizeParkHour(Park, Array.Empty<SimulatedInverter>(), Utc(2026, 11, 15, 12));

        Assert.Equal(0.0, summary.EnergyKwh);
        Assert.Equal(0.0, summary.PeakPowerKw);
    }

    [Fact]
    public void Summarize_WithAnExtraInverter_GeneratesMore()
    {
        var hourStart = Utc(2026, 11, 15, 12);

        var one = _engine.SummarizeParkHour(Park, Inverters.Take(1).ToArray(), hourStart);
        var two = _engine.SummarizeParkHour(Park, Inverters, hourStart);

        Assert.True(two.EnergyKwh > one.EnergyKwh);
    }

    [Fact]
    public void Summarize_WhenTheHourDoesNotStartOnAWholeHour_Throws()
    {
        Assert.Throws<ArgumentException>(() => _engine.SummarizeParkHour(Park, Inverters, Utc(2026, 11, 15, 12).AddMinutes(30)));
    }

    [Fact]
    public void Summarize_IsDeterministic()
    {
        var hourStart = Utc(2026, 11, 15, 9);

        Assert.Equal(_engine.SummarizeParkHour(Park, Inverters, hourStart), _engine.SummarizeParkHour(Park, Inverters, hourStart));
    }
}
