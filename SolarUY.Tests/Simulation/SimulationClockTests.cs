using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class SimulationClockTests
{
    private static DateTime Utc(int hour, int minute, int second, int millisecond = 0)
        => new DateTime(2026, 11, 15, hour, minute, second, millisecond, DateTimeKind.Utc);

    [Theory]
    [InlineData(12, 0, 0, 0, 12, 0, 0)]
    [InlineData(12, 0, 4, 999, 12, 0, 0)]
    [InlineData(12, 0, 5, 0, 12, 0, 5)]
    [InlineData(12, 0, 9, 500, 12, 0, 5)]
    [InlineData(12, 59, 59, 999, 12, 59, 55)]
    public void Snap_RoundsDownToAMultipleOfTheInterval(int h, int m, int s, int ms, int eh, int em, int es)
    {
        Assert.Equal(Utc(eh, em, es), SimulationClock.Snap(Utc(h, m, s, ms), 5));
    }

    [Fact]
    public void Snap_ReturnsAUtcDate()
    {
        var snapped = SimulationClock.Snap(DateTime.SpecifyKind(Utc(12, 0, 7), DateTimeKind.Unspecified), 5);

        Assert.Equal(DateTimeKind.Utc, snapped.Kind);
    }

    [Fact]
    public void Snap_TwoInstantsOfTheSameCycleGiveTheSameInstant()
    {
        Assert.Equal(SimulationClock.Snap(Utc(12, 0, 5, 40), 5), SimulationClock.Snap(Utc(12, 0, 9, 900), 5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Snap_WithANonPositiveInterval_UsesOneSecond(int interval)
    {
        Assert.Equal(Utc(12, 0, 7), SimulationClock.Snap(Utc(12, 0, 7, 600), interval));
    }

    [Fact]
    public void DelayUntilNextCycle_WaitsForTheNextMultipleOfTheInterval()
    {
        var delay = SimulationClock.DelayUntilNextCycle(Utc(12, 0, 2, 0), 5);

        Assert.Equal(TimeSpan.FromSeconds(3) + SimulationClock.CycleMargin, delay);
    }

    [Fact]
    public void DelayUntilNextCycle_JustAfterABoundary_WaitsAlmostOneFullInterval()
    {
        var delay = SimulationClock.DelayUntilNextCycle(Utc(12, 0, 5, 100), 5);

        Assert.Equal(TimeSpan.FromMilliseconds(4900) + SimulationClock.CycleMargin, delay);
    }

    [Fact]
    public void DelayUntilNextCycle_WakingUpAfterTheDelay_SnapsToTheBoundary()
    {
        var now = Utc(12, 0, 2, 300);

        var wake = now + SimulationClock.DelayUntilNextCycle(now, 5);

        Assert.Equal(Utc(12, 0, 5), SimulationClock.Snap(wake, 5));
    }

    [Fact]
    public void DelayUntilNextCycle_IsAlwaysPositiveAndNeverLongerThanTheIntervalPlusMargin()
    {
        for (var ms = 0; ms < 5000; ms += 137)
        {
            var delay = SimulationClock.DelayUntilNextCycle(Utc(12, 0, 0).AddMilliseconds(ms), 5);

            Assert.True(delay > TimeSpan.Zero);
            Assert.True(delay <= TimeSpan.FromSeconds(5) + SimulationClock.CycleMargin);
        }
    }
}
