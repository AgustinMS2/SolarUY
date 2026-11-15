using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

/// <summary>Cómo las lecturas y las alertas de las specs 028 a 031 reflejan las fallas simuladas (spec 037).</summary>
public class SimulatedFaultReadingsTests
{
    // 30 de octubre, mediodía solar (12:45): la potencia esperada de un panel de 400 W es 400 W.
    private static readonly DateTime LocalNoon = new(2026, 10, 30, 12, 45, 0);
    private static readonly DateTime LocalNight = new(2026, 10, 30, 23, 0, 0);

    private readonly PanelTelemetryService _panelTelemetry = new();
    private readonly PanelHealthService _health = new(new PanelTelemetryService());
    private readonly NodeTelemetryService _nodeTelemetry = new();

    private static SolarPanel Panel() => new()
    {
        Id = Guid.NewGuid(),
        SerieNumber = "PNL-0001",
        NormalizedSerieNumber = "pnl-0001",
        PowerW = 400,
        Efficiency = 20,
        SolarParkId = Guid.NewGuid(),
        SolarNodeId = Guid.NewGuid(),
        Status = SolarPanelStatus.Active
    };

    private static SolarNode Node() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Nodo Norte",
        NormalizedName = "nodo norte",
        SolarParkId = Guid.NewGuid(),
        Status = SolarNodeStatus.Active
    };

    private static Inverter Inverter() => new()
    {
        Id = Guid.NewGuid(),
        SerieNumber = "INV-0001",
        NormalizedSerieNumber = "inv-0001",
        NominalCapacityKw = 110
    };

    // ---------- Panel (spec 029) ----------

    [Fact]
    public void PanelTelemetry_CopiesTheActiveFaultAndItsLabel()
    {
        var dto = _panelTelemetry.CalculateMetrics(Panel(), "Parque", "Nodo", 0.0, LocalNoon, DateTime.UtcNow, SimulatedFaultType.OpenCircuit);

        Assert.Equal(SimulatedFaultType.OpenCircuit, dto.Fault);
        Assert.Equal("Corte total (circuito abierto)", dto.FaultLabel);
    }

    [Fact]
    public void PanelTelemetry_WithoutFault_HasNoFault()
    {
        var dto = _panelTelemetry.CalculateMetrics(Panel(), "Parque", "Nodo", 380.0, LocalNoon, DateTime.UtcNow);

        Assert.Null(dto.Fault);
        Assert.Null(dto.FaultLabel);
    }

    // ---------- Mapa de salud (spec 030) ----------

    [Fact]
    public void Health_OpenCircuit_IsCriticalWithTheFaultAsReason()
    {
        var reading = _health.ClassifyHealth(Panel(), true, 0.0, LocalNoon, SimulatedFaultType.OpenCircuit);

        Assert.Equal(PanelHealthState.Critical, reading.State);
        Assert.Equal(SimulatedFaultType.OpenCircuit, reading.Fault);
        Assert.Equal("Falla simulada: Corte total (circuito abierto)", reading.StateReason);
        Assert.Equal("Corte total (circuito abierto)", reading.FaultLabel);
    }

    [Fact]
    public void Health_SevereDegradation_IsAmberWithLowPerformance()
    {
        var reading = _health.ClassifyHealth(Panel(), true, 120.0, LocalNoon, SimulatedFaultType.SevereDegradation);

        Assert.Equal(PanelHealthState.Degraded, reading.State);
        Assert.True(reading.IsLowPerformance);
        Assert.Equal(SimulatedFaultType.SevereDegradation, reading.Fault);
        Assert.Equal("Falla simulada: Degradación severa / suciedad", reading.StateReason);
    }

    [Fact]
    public void Health_WithoutFault_HasNoFaultNorReason()
    {
        var reading = _health.ClassifyHealth(Panel(), true, 400.0, LocalNoon);

        Assert.Null(reading.Fault);
        Assert.Null(reading.StateReason);
    }

    [Fact]
    public void Health_AtNightAFaultStaysRecordedButTheStateIsStillNoSun()
    {
        var reading = _health.ClassifyHealth(Panel(), true, 0.0, LocalNight, SimulatedFaultType.OpenCircuit);

        Assert.Equal(PanelHealthState.NoSun, reading.State);
        Assert.Equal(SimulatedFaultType.OpenCircuit, reading.Fault);
    }

    [Fact]
    public void Health_APanelWithoutReadingsDoesNotShowAFault()
    {
        var panel = Panel();
        panel.Status = SolarPanelStatus.Inactive;

        var reading = _health.ClassifyHealth(panel, true, 0.0, LocalNoon, SimulatedFaultType.OpenCircuit);

        Assert.Equal(PanelHealthState.Inactive, reading.State);
        Assert.Null(reading.Fault);
        Assert.Equal("Panel dado de baja", reading.StateReason);
    }

    // ---------- Nodo (spec 028) ----------

    [Fact]
    public void NodeTelemetry_InsulationFault_ReportsAShutDownInverterWithoutSpuriousAlarms()
    {
        var dto = _nodeTelemetry.CalculateMetrics(Node(), Inverter(), 0.0, 8.0, 0.0, 0.0, 0.0, 95.0, DateTime.UtcNow, SimulatedFaultType.InsulationFault);

        Assert.Equal(SimulatedFaultType.InsulationFault, dto.Fault);
        Assert.Equal("Falla de aislamiento", dto.FaultLabel);
        Assert.Equal(0.0, dto.ActivePowerKw);
        Assert.Equal(0.0, dto.AcVoltage);
        Assert.Equal(0.0, dto.GridFrequencyHz);
        Assert.Equal(0.0, dto.InverterTemperatureC);
        Assert.False(dto.IsOverheating);
        Assert.False(dto.IsGridFrequencyAnomaly);
    }

    [Fact]
    public void NodeTelemetry_OverTemperature_KeepsTheReadingsAndRaisesTheOverheatingFlag()
    {
        var dto = _nodeTelemetry.CalculateMetrics(Node(), Inverter(), 80.0, 600.0, 130.0, 380.0, 50.0, 88.0, DateTime.UtcNow, SimulatedFaultType.InverterOverTemperature);

        Assert.Equal(SimulatedFaultType.InverterOverTemperature, dto.Fault);
        Assert.Equal(80.0, dto.ActivePowerKw);
        Assert.Equal(88.0, dto.InverterTemperatureC);
        Assert.True(dto.IsOverheating);
    }

    [Fact]
    public void NodeTelemetry_WithoutFault_HasNoFault()
    {
        var dto = _nodeTelemetry.CalculateMetrics(Node(), Inverter(), 80.0, 600.0, 130.0, 380.0, 50.0, 60.0, DateTime.UtcNow);

        Assert.Null(dto.Fault);
        Assert.Null(dto.FaultLabel);
    }

    // ---------- Alertas (spec 031) ----------

    private static NodeTelemetryDto NodeDto(SimulatedFaultType? fault, bool overheating = false, double temperatureC = 60.0) => new()
    {
        NodeId = Guid.NewGuid(),
        NodeName = "Nodo Norte",
        InverterTemperatureC = temperatureC,
        IsOverheating = overheating,
        Fault = fault
    };

    [Fact]
    public void Alerts_InsulationFault_IsACriticalAlertOnTheNode()
    {
        var telemetry = NodeDto(SimulatedFaultType.InsulationFault);

        var detection = AlertDetectionRules.FromNodeTelemetry(telemetry, Guid.NewGuid(), "Parque Salto");

        Assert.NotNull(detection);
        Assert.Equal(AlertSeverity.Critical, detection!.Severity);
        Assert.Equal(AlertEquipmentType.Node, detection.EquipmentType);
        Assert.Equal(telemetry.NodeId, detection.EquipmentId);
        Assert.Equal("Nodo Norte", detection.EquipmentName);
        Assert.Equal("Falla de aislamiento en inversor: protección de fuga a tierra, inversor apagado.", detection.Description);
    }

    [Fact]
    public void Alerts_InsulationFaultPrevailsOverTheOverheating()
    {
        var detection = AlertDetectionRules.FromNodeTelemetry(NodeDto(SimulatedFaultType.InsulationFault, overheating: true, temperatureC: 90), Guid.NewGuid(), "Parque Salto");

        Assert.Equal(AlertSeverity.Critical, detection!.Severity);
    }

    [Fact]
    public void Alerts_OverTemperatureFault_IsTheExistingWarning()
    {
        var detection = AlertDetectionRules.FromNodeTelemetry(NodeDto(SimulatedFaultType.InverterOverTemperature, overheating: true, temperatureC: 88), Guid.NewGuid(), "Parque Salto");

        Assert.Equal(AlertSeverity.Warning, detection!.Severity);
        Assert.Contains("Temperatura alta en inversor", detection.Description);
    }

    private static PanelHealthReadingDto HealthReading(PanelHealthState state, SimulatedFaultType? fault, double? power = 0.0, double expected = 267.0) => new()
    {
        PanelId = Guid.NewGuid(),
        State = state,
        ActivePowerW = power,
        ExpectedPowerW = expected,
        Fault = fault
    };

    [Fact]
    public void Alerts_OpenCircuit_IsTheCriticalPanelAlertAndNamesTheFault()
    {
        var detection = AlertDetectionRules.FromPanelHealth(HealthReading(PanelHealthState.Critical, SimulatedFaultType.OpenCircuit), Guid.NewGuid(), "Parque Salto", "PNL-0001");

        Assert.Equal(AlertSeverity.Critical, detection!.Severity);
        Assert.Equal(AlertEquipmentType.Panel, detection.EquipmentType);
        Assert.Contains("Panel sin generación con sol", detection.Description);
        Assert.Contains("Corte total (circuito abierto)", detection.Description);
    }

    [Fact]
    public void Alerts_CriticalPanelWithoutAFault_KeepsTheOriginalDescription()
    {
        var detection = AlertDetectionRules.FromPanelHealth(HealthReading(PanelHealthState.Critical, null), Guid.NewGuid(), "Parque Salto", "PNL-0001");

        Assert.DoesNotContain("Falla", detection!.Description);
    }

    [Fact]
    public void Alerts_SevereDegradation_IsAWarningOnThePanel()
    {
        var reading = HealthReading(PanelHealthState.Degraded, SimulatedFaultType.SevereDegradation, power: 80.1);

        var detection = AlertDetectionRules.FromPanelHealth(reading, Guid.NewGuid(), "Parque Salto", "PNL-0001");

        Assert.NotNull(detection);
        Assert.Equal(AlertSeverity.Warning, detection!.Severity);
        Assert.Equal(AlertEquipmentType.Panel, detection.EquipmentType);
        Assert.Equal(reading.PanelId, detection.EquipmentId);
        Assert.Equal("PNL-0001", detection.EquipmentName);
        Assert.Contains("Degradación severa del panel", detection.Description);
        Assert.Contains("esperados", detection.Description);
    }

    [Fact]
    public void Alerts_DegradedPanelWithoutAFault_DoesNotAlert()
    {
        // Un sombreado ocasional de la simulación no es una falla: no genera alertas.
        Assert.Null(AlertDetectionRules.FromPanelHealth(HealthReading(PanelHealthState.Degraded, null, power: 80.0), Guid.NewGuid(), "Parque", "PNL-0001"));
    }

    [Fact]
    public void Alerts_NormalPanel_DoesNotAlertEvenIfAFaultIsStillRecorded()
    {
        Assert.Null(AlertDetectionRules.FromPanelHealth(HealthReading(PanelHealthState.NoSun, SimulatedFaultType.OpenCircuit), Guid.NewGuid(), "Parque", "PNL-0001"));
    }
}
