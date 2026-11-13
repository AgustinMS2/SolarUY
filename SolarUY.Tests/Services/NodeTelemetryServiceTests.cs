using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class NodeTelemetryServiceTests
{
    private readonly NodeTelemetryService _service = new();

    private static SolarNode CreateActiveNode()
    {
        return new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo Norte",
            NormalizedName = "nodo norte",
            SolarParkId = Guid.NewGuid(),
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Inverter CreateInverter(double nominalCapacityKw = 25)
    {
        return new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = "INV-2024-0001",
            NormalizedSerieNumber = "inv-2024-0001",
            BrandAndModel = "Huawei SUN2000-25KTL",
            NominalCapacityKw = nominalCapacityKw,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public void CalculateMetrics_WithNullNode_ThrowsArgumentNullException()
    {
        var inverter = CreateInverter();

        Assert.Throws<ArgumentNullException>(() =>
            _service.CalculateMetrics(null!, inverter, 20, 600, 33, 380, 50, 45, DateTime.UtcNow));
    }

    [Fact]
    public void CalculateMetrics_WithActiveNodeAndInverter_BuildsTelemetryWithElectricMetrics()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter(nominalCapacityKw: 25);
        var timestamp = new DateTime(2026, 11, 13, 12, 30, 0, DateTimeKind.Utc);

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50, temperatureC: 45,
            timestampUtc: timestamp);

        Assert.Equal(node.Id, result.NodeId);
        Assert.Equal("Nodo Norte", result.NodeName);
        Assert.Equal("INV-2024-0001", result.InverterSerieNumber);
        Assert.Equal(25, result.NominalCapacityKw);
        Assert.Equal(20, result.ActivePowerKw);
        Assert.Equal(600, result.DcVoltage);
        Assert.Equal(33, result.DcCurrent);
        Assert.Equal(380, result.AcVoltage);
        Assert.Equal(50, result.GridFrequencyHz);
        Assert.Equal(45, result.InverterTemperatureC);
        Assert.False(result.IsOverheating);
        Assert.False(result.IsGridFrequencyAnomaly);
        Assert.Equal(timestamp, result.TimestampUtc);
    }

    [Fact]
    public void CalculateMetrics_WhenTemperatureAbove75_DetectsOverheating()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter();

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50, temperatureC: 78.5,
            timestampUtc: DateTime.UtcNow);

        Assert.True(result.IsOverheating);
        Assert.Equal(78.5, result.InverterTemperatureC);
    }

    [Fact]
    public void CalculateMetrics_WhenTemperatureIsExactly75_DoesNotDetectOverheating()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter();

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50, temperatureC: 75.0,
            timestampUtc: DateTime.UtcNow);

        Assert.False(result.IsOverheating);
    }

    [Fact]
    public void CalculateMetrics_WhenFrequencyBelowRange_DetectsGridFrequencyAnomaly()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter();

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 49.2, temperatureC: 45,
            timestampUtc: DateTime.UtcNow);

        Assert.True(result.IsGridFrequencyAnomaly);
    }

    [Fact]
    public void CalculateMetrics_WhenFrequencyAboveRange_DetectsGridFrequencyAnomaly()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter();

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50.8, temperatureC: 45,
            timestampUtc: DateTime.UtcNow);

        Assert.True(result.IsGridFrequencyAnomaly);
    }

    [Fact]
    public void CalculateMetrics_WhenFrequencyIsAtRangeLimits_DoesNotDetectAnomaly()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter();

        var atLowerLimit = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 49.5, temperatureC: 45,
            timestampUtc: DateTime.UtcNow);

        var atUpperLimit = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50.5, temperatureC: 45,
            timestampUtc: DateTime.UtcNow);

        Assert.False(atLowerLimit.IsGridFrequencyAnomaly);
        Assert.False(atUpperLimit.IsGridFrequencyAnomaly);
    }

    [Fact]
    public void CalculateMetrics_WhenFrequencyIsNominal_DoesNotDetectAnomaly()
    {
        var node = CreateActiveNode();
        var inverter = CreateInverter();

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50.0, temperatureC: 45,
            timestampUtc: DateTime.UtcNow);

        Assert.False(result.IsGridFrequencyAnomaly);
    }

    [Fact]
    public void CalculateMetrics_WhenNodeIsInactive_SetsPowerAndCurrentToZeroAndSuppressesAlerts()
    {
        var node = CreateActiveNode();
        node.Status = SolarNodeStatus.Inactive;
        var inverter = CreateInverter();

        var result = _service.CalculateMetrics(
            node, inverter,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 49.2, temperatureC: 80,
            timestampUtc: DateTime.UtcNow);

        Assert.Equal(0.0, result.ActivePowerKw);
        Assert.Equal(0.0, result.DcCurrent);
        Assert.False(result.IsOverheating);
        Assert.False(result.IsGridFrequencyAnomaly);
    }

    [Fact]
    public void CalculateMetrics_WhenNodeHasNoInverter_SetsPowerAndCurrentToZeroAndSuppressesAlerts()
    {
        var node = CreateActiveNode();

        var result = _service.CalculateMetrics(
            node, inverter: null,
            activePowerKw: 20, dcVoltage: 600, dcCurrent: 33,
            acVoltage: 380, frequencyHz: 50.8, temperatureC: 80,
            timestampUtc: DateTime.UtcNow);

        Assert.Equal(string.Empty, result.InverterSerieNumber);
        Assert.Equal(0.0, result.NominalCapacityKw);
        Assert.Equal(0.0, result.ActivePowerKw);
        Assert.Equal(0.0, result.DcCurrent);
        Assert.False(result.IsOverheating);
        Assert.False(result.IsGridFrequencyAnomaly);
    }
}
