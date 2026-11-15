using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class AlertDetectionRulesTests
{
    private static readonly Guid ParkId = Guid.NewGuid();

    // ---------- Parque bajo su umbral (spec 027) ----------

    private static ParkTelemetryDto ParkTelemetry(bool alertActive, double powerKw = 12.3, double thresholdKw = 75) => new()
    {
        ParkId = ParkId,
        ParkName = "Parque Salto",
        CurrentPowerKw = powerKw,
        AlertThresholdKw = thresholdKw,
        IsAlertActive = alertActive
    };

    [Fact]
    public void FromParkTelemetry_WithoutActiveAlert_DetectsNothing()
    {
        Assert.Null(AlertDetectionRules.FromParkTelemetry(ParkTelemetry(alertActive: false)));
    }

    [Fact]
    public void FromParkTelemetry_WithActiveAlert_IsAWarningOnThePark()
    {
        var fault = AlertDetectionRules.FromParkTelemetry(ParkTelemetry(alertActive: true));

        Assert.NotNull(fault);
        Assert.Equal(AlertSeverity.Warning, fault!.Severity);
        Assert.Equal(AlertEquipmentType.Park, fault.EquipmentType);
        Assert.Equal(ParkId, fault.EquipmentId);
        Assert.Equal(ParkId, fault.ParkId);
        Assert.Equal("Parque Salto", fault.EquipmentName);
        Assert.Equal("Parque Salto", fault.ParkName);
    }

    [Fact]
    public void FromParkTelemetry_DescribesTheThresholdAndTheCurrentPower()
    {
        var fault = AlertDetectionRules.FromParkTelemetry(ParkTelemetry(alertActive: true, powerKw: 12.3, thresholdKw: 75));

        Assert.Contains("Generación por debajo del umbral de alerta (75 kW)", fault!.Description);
        Assert.Contains("12", fault.Description);
    }

    [Fact]
    public void FromParkTelemetry_WithNullTelemetry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AlertDetectionRules.FromParkTelemetry(null!));
    }

    // ---------- Inversor sobre 75 °C (spec 028) ----------

    private static NodeTelemetryDto NodeTelemetry(bool overheating, double temperatureC = 78.4) => new()
    {
        NodeId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        NodeName = "Nodo Norte",
        InverterTemperatureC = temperatureC,
        IsOverheating = overheating
    };

    [Fact]
    public void FromNodeTelemetry_WithoutOverheating_DetectsNothing()
    {
        Assert.Null(AlertDetectionRules.FromNodeTelemetry(NodeTelemetry(overheating: false, temperatureC: 70), ParkId, "Parque Salto"));
    }

    [Fact]
    public void FromNodeTelemetry_Overheating_IsAWarningOnTheNode()
    {
        var telemetry = NodeTelemetry(overheating: true);

        var fault = AlertDetectionRules.FromNodeTelemetry(telemetry, ParkId, "Parque Salto");

        Assert.NotNull(fault);
        Assert.Equal(AlertSeverity.Warning, fault!.Severity);
        Assert.Equal(AlertEquipmentType.Node, fault.EquipmentType);
        Assert.Equal(telemetry.NodeId, fault.EquipmentId);
        Assert.Equal("Nodo Norte", fault.EquipmentName);
        Assert.Equal(ParkId, fault.ParkId);
        Assert.Equal("Parque Salto", fault.ParkName);
    }

    [Fact]
    public void FromNodeTelemetry_DescribesTheTemperature()
    {
        var fault = AlertDetectionRules.FromNodeTelemetry(NodeTelemetry(overheating: true, temperatureC: 78.4), ParkId, "Parque Salto");

        Assert.Contains("Temperatura alta en inversor", fault!.Description);
        Assert.Contains("78", fault.Description);
        Assert.Contains("°C", fault.Description);
    }

    [Fact]
    public void FromNodeTelemetry_WithNullTelemetry_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AlertDetectionRules.FromNodeTelemetry(null!, ParkId, "Parque Salto"));
    }

    // ---------- Panel en Falla Crítica (spec 030) ----------

    private static PanelHealthReadingDto Reading(PanelHealthState state, double? power = 12.0, double expected = 400.0) => new()
    {
        PanelId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
        State = state,
        ActivePowerW = power,
        ExpectedPowerW = expected
    };

    [Fact]
    public void FromPanelHealth_CriticalPanel_IsACriticalAlertOnThePanel()
    {
        var reading = Reading(PanelHealthState.Critical);

        var fault = AlertDetectionRules.FromPanelHealth(reading, ParkId, "Parque Salto", "PNL-0042");

        Assert.NotNull(fault);
        Assert.Equal(AlertSeverity.Critical, fault!.Severity);
        Assert.Equal(AlertEquipmentType.Panel, fault.EquipmentType);
        Assert.Equal(reading.PanelId, fault.EquipmentId);
        Assert.Equal("PNL-0042", fault.EquipmentName);
        Assert.Equal(ParkId, fault.ParkId);
        Assert.Equal("Parque Salto", fault.ParkName);
    }

    [Fact]
    public void FromPanelHealth_DescribesTheGenerationAgainstTheExpectedPower()
    {
        var fault = AlertDetectionRules.FromPanelHealth(Reading(PanelHealthState.Critical, power: 12.0, expected: 400.0), ParkId, "Parque Salto", "PNL-0042");

        Assert.Contains("Panel sin generación con sol", fault!.Description);
        Assert.Contains("12", fault.Description);
        Assert.Contains("400", fault.Description);
    }

    [Theory]
    [InlineData(PanelHealthState.Normal)]
    [InlineData(PanelHealthState.Degraded)]
    [InlineData(PanelHealthState.NoSun)]
    [InlineData(PanelHealthState.Inactive)]
    public void FromPanelHealth_AnyStateOtherThanCritical_DetectsNothing(PanelHealthState state)
    {
        Assert.Null(AlertDetectionRules.FromPanelHealth(Reading(state), ParkId, "Parque Salto", "PNL-0042"));
    }

    [Fact]
    public void FromPanelHealth_WithNullReading_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AlertDetectionRules.FromPanelHealth(null!, ParkId, "Parque Salto", "PNL-0042"));
    }
}
