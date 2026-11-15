using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Simulation;
using Xunit;

namespace SolarUY.Tests.Simulation;

public class FaultCatalogTests
{
    [Theory]
    [InlineData(SimulatedFaultType.OpenCircuit, SimulatedFaultTarget.Panel)]
    [InlineData(SimulatedFaultType.SevereDegradation, SimulatedFaultTarget.Panel)]
    [InlineData(SimulatedFaultType.InverterOverTemperature, SimulatedFaultTarget.Inverter)]
    [InlineData(SimulatedFaultType.InsulationFault, SimulatedFaultTarget.Inverter)]
    public void TargetOf_EachFaultAppliesToTheEquipmentOfTheSpecTable(SimulatedFaultType type, SimulatedFaultTarget expected)
    {
        Assert.Equal(expected, FaultCatalog.TargetOf(type));
    }

    [Theory]
    [InlineData(SimulatedFaultType.OpenCircuit, AlertSeverity.Critical)]
    [InlineData(SimulatedFaultType.InsulationFault, AlertSeverity.Critical)]
    [InlineData(SimulatedFaultType.SevereDegradation, AlertSeverity.Warning)]
    [InlineData(SimulatedFaultType.InverterOverTemperature, AlertSeverity.Warning)]
    public void SeverityOf_FollowsTheAlertLevelOfTheSpecTable(SimulatedFaultType type, AlertSeverity expected)
    {
        Assert.Equal(expected, FaultCatalog.SeverityOf(type));
    }

    [Theory]
    [InlineData(SimulatedFaultType.OpenCircuit, "Corte total (circuito abierto)")]
    [InlineData(SimulatedFaultType.SevereDegradation, "Degradación severa / suciedad")]
    [InlineData(SimulatedFaultType.InverterOverTemperature, "Sobretemperatura de inversor")]
    [InlineData(SimulatedFaultType.InsulationFault, "Falla de aislamiento")]
    public void LabelOf_IsTheSpanishNameOfTheSpecTable(SimulatedFaultType type, string expected)
    {
        Assert.Equal(expected, FaultCatalog.LabelOf(type));
    }

    [Fact]
    public void TypesFor_Panel_AreTheTwoPanelFaults()
    {
        Assert.Equal(
            new[] { SimulatedFaultType.OpenCircuit, SimulatedFaultType.SevereDegradation },
            FaultCatalog.TypesFor(SimulatedFaultTarget.Panel).OrderBy(t => (int)t));
    }

    [Fact]
    public void TypesFor_Inverter_AreTheTwoInverterFaults()
    {
        Assert.Equal(
            new[] { SimulatedFaultType.InverterOverTemperature, SimulatedFaultType.InsulationFault },
            FaultCatalog.TypesFor(SimulatedFaultTarget.Inverter).OrderBy(t => (int)t));
    }

    [Fact]
    public void EveryFaultTypeBelongsToExactlyOneEquipmentType()
    {
        foreach (var type in Enum.GetValues<SimulatedFaultType>())
        {
            var owners = Enum.GetValues<SimulatedFaultTarget>().Count(target => FaultCatalog.TypesFor(target).Contains(type));
            Assert.Equal(1, owners);
        }
    }

    [Fact]
    public void UnknownFaultType_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FaultCatalog.TargetOf((SimulatedFaultType)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => FaultCatalog.SeverityOf((SimulatedFaultType)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => FaultCatalog.LabelOf((SimulatedFaultType)99));
    }
}
