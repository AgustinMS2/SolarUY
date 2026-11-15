using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class FaultTimelineTests
{
    private static readonly Guid PanelA = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
    private static readonly Guid PanelB = Guid.Parse("cccccccc-0000-0000-0000-000000000002");

    private static DateTime Utc(int hour, int minute = 0, int second = 0)
        => new(2026, 11, 15, hour, minute, second, DateTimeKind.Utc);

    private static FaultWindow Window(Guid target, SimulatedFaultType type, DateTime start, DateTime? end = null)
        => new(target, type, start, end);

    [Fact]
    public void EffectiveFault_EmptyTimeline_HasNoFault()
    {
        Assert.Null(FaultTimeline.Empty.EffectiveFault(PanelA, Utc(12)));
        Assert.True(FaultTimeline.Empty.IsEmpty);
    }

    [Fact]
    public void EffectiveFault_ADefaultTimelineBuiltFromNothing_IsEmpty()
    {
        Assert.True(new FaultTimeline([]).IsEmpty);
    }

    [Fact]
    public void EffectiveFault_StartsExactlyAtTheStartInstant()
    {
        var timeline = new FaultTimeline([Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(12))]);

        Assert.Null(timeline.EffectiveFault(PanelA, Utc(11, 59, 59)));
        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(12)));
        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(23)));
    }

    [Fact]
    public void EffectiveFault_EndsExactlyAtTheEndInstant()
    {
        var timeline = new FaultTimeline([Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(12), Utc(13))]);

        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(12, 59, 59)));
        Assert.Null(timeline.EffectiveFault(PanelA, Utc(13)));
        Assert.Null(timeline.EffectiveFault(PanelA, Utc(14)));
    }

    [Fact]
    public void EffectiveFault_OnlyAppliesToTheEquipmentOfTheFault()
    {
        var timeline = new FaultTimeline([Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(12))]);

        Assert.Null(timeline.EffectiveFault(PanelB, Utc(13)));
    }

    [Fact]
    public void EffectiveFault_TheCriticalFaultPrevailsOverTheWarning_WhateverTheOrder()
    {
        var degradationFirst = new FaultTimeline([
            Window(PanelA, SimulatedFaultType.SevereDegradation, Utc(12)),
            Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(13))]);
        var criticalFirst = new FaultTimeline([
            Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(12)),
            Window(PanelA, SimulatedFaultType.SevereDegradation, Utc(13))]);

        Assert.Equal(SimulatedFaultType.OpenCircuit, degradationFirst.EffectiveFault(PanelA, Utc(14)));
        Assert.Equal(SimulatedFaultType.OpenCircuit, criticalFirst.EffectiveFault(PanelA, Utc(14)));
    }

    [Fact]
    public void EffectiveFault_WhenTheCriticalFaultEnds_TheWarningPrevailsAgain()
    {
        var timeline = new FaultTimeline([
            Window(PanelA, SimulatedFaultType.SevereDegradation, Utc(12)),
            Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(13), Utc(14))]);

        Assert.Equal(SimulatedFaultType.SevereDegradation, timeline.EffectiveFault(PanelA, Utc(12, 30)));
        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(13, 30)));
        Assert.Equal(SimulatedFaultType.SevereDegradation, timeline.EffectiveFault(PanelA, Utc(14, 30)));
    }

    [Fact]
    public void EffectiveFault_TwoFaultsOfTheSameSeverity_TheMostRecentPrevails()
    {
        var timeline = new FaultTimeline([
            Window(PanelA, SimulatedFaultType.InverterOverTemperature, Utc(12)),
            Window(PanelA, SimulatedFaultType.SevereDegradation, Utc(13))]);

        Assert.Equal(SimulatedFaultType.SevereDegradation, timeline.EffectiveFault(PanelA, Utc(14)));
    }

    [Fact]
    public void EffectiveFault_TheSameFaultInjectedAgainAfterBeingCleared_AppliesInBothPeriods()
    {
        var timeline = new FaultTimeline([
            Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(10), Utc(11)),
            Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(13))]);

        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(10, 30)));
        Assert.Null(timeline.EffectiveFault(PanelA, Utc(12)));
        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(13, 30)));
    }

    [Fact]
    public void EffectiveFault_IgnoresTheKindOfTheDates()
    {
        var timeline = new FaultTimeline([Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(12))]);
        var unspecified = DateTime.SpecifyKind(Utc(13), DateTimeKind.Unspecified);

        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, unspecified));
    }

    [Fact]
    public void EffectiveFault_AFaultThatEndsBeforeItStarts_NeverApplies()
    {
        var timeline = new FaultTimeline([Window(PanelA, SimulatedFaultType.OpenCircuit, Utc(13), Utc(12))]);

        Assert.Null(timeline.EffectiveFault(PanelA, Utc(12, 30)));
        Assert.Null(timeline.EffectiveFault(PanelA, Utc(14)));
    }

    [Fact]
    public void From_BuildsTheTimelineWithTheStoredFaultsActiveAndEnded()
    {
        var faults = new[]
        {
            new SimulatedFault { TargetId = PanelA, FaultType = SimulatedFaultType.OpenCircuit, StartedAtUtc = Utc(10), EndedAtUtc = Utc(11) },
            new SimulatedFault { TargetId = PanelB, FaultType = SimulatedFaultType.SevereDegradation, StartedAtUtc = Utc(10) }
        };

        var timeline = FaultTimeline.From(faults);

        Assert.Equal(SimulatedFaultType.OpenCircuit, timeline.EffectiveFault(PanelA, Utc(10, 30)));
        Assert.Null(timeline.EffectiveFault(PanelA, Utc(11, 30)));
        Assert.Equal(SimulatedFaultType.SevereDegradation, timeline.EffectiveFault(PanelB, Utc(12)));
    }

    [Fact]
    public void From_NoFaults_IsEmpty()
    {
        Assert.True(FaultTimeline.From([]).IsEmpty);
    }
}
