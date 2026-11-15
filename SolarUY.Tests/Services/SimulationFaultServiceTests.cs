using Microsoft.Extensions.Options;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.ApiService.Simulation;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SimulationFaultServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeSimulatedFaultRepository _faults = new();
    private readonly FakeSolarPanelRepository _panels = new();
    private readonly FakeSolarNodeRepository _nodes = new();
    private readonly FakeInverterRepository _inverters = new();
    private readonly FakeSolarParkRepository _parks = new();
    private readonly SimulationOptions _options = new() { FaultInjectionEnabled = true };
    private readonly SimulationFaultService _service;

    private readonly SolarPark _park;
    private readonly SolarNode _node;
    private readonly SolarPanel _panel;
    private readonly Inverter _inverter;

    public SimulationFaultServiceTests()
    {
        _service = new SimulationFaultService(_faults, _panels, _nodes, _inverters, _parks, Options.Create(_options), new TestTimeProvider(Now));

        _park = new SolarPark { Id = Guid.NewGuid(), Name = "Parque Salto", NormalizedName = "parque salto", Status = SolarParkStatus.Active };
        _parks.Parks.Add(_park);
        _node = new SolarNode { Id = Guid.NewGuid(), Name = "Nodo Norte", NormalizedName = "nodo norte", SolarParkId = _park.Id, SolarPark = _park, Status = SolarNodeStatus.Active };
        _nodes.PersistedNodes.Add(_node);
        _inverter = new Inverter { Id = Guid.NewGuid(), SerieNumber = "INV-001", NormalizedSerieNumber = "inv-001", NominalCapacityKw = 110, SolarNodeId = _node.Id, SolarNode = _node };
        _inverters.Inverters.Add(_inverter);
        _panel = AddPanel("PNL-001", _node);
    }

    private SolarPanel AddPanel(string serie, SolarNode? node, SolarPanelStatus status = SolarPanelStatus.Active)
    {
        var panel = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            PowerW = 400,
            Efficiency = 20,
            SolarParkId = _park.Id,
            SolarPark = _park,
            SolarNodeId = node?.Id,
            SolarNode = node,
            Status = status
        };
        _panels.Panels.Add(panel);
        return panel;
    }

    // ---------- Inyectar ----------

    [Fact]
    public void Inject_ValidPanelFault_StoresItAndReturnsTheSpecMessage()
    {
        var result = _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.True(result.Success);
        Assert.Equal("Falla simulada inyectada con éxito en el panel \"PNL-001\".", result.Value);
        var fault = Assert.Single(_faults.Faults);
        Assert.Equal(SimulatedFaultType.OpenCircuit, fault.FaultType);
        Assert.Equal(SimulatedFaultTarget.Panel, fault.TargetType);
        Assert.Equal(_panel.Id, fault.TargetId);
        Assert.Equal(_park.Id, fault.SolarParkId);
        Assert.Equal(Now.UtcDateTime, fault.StartedAtUtc);
        Assert.Null(fault.EndedAtUtc);
        Assert.Equal("admin", fault.StartedBy);
        Assert.NotEqual(Guid.Empty, fault.Id);
    }

    [Fact]
    public void Inject_ValidInverterFault_StoresItAndReturnsTheInverterMessage()
    {
        var result = _service.Inject(SimulatedFaultTarget.Inverter, _inverter.Id, SimulatedFaultType.InsulationFault, "admin");

        Assert.True(result.Success);
        Assert.Equal("Falla simulada inyectada con éxito en el inversor \"INV-001\".", result.Value);
        Assert.Equal(_park.Id, Assert.Single(_faults.Faults).SolarParkId);
    }

    [Fact]
    public void Inject_WhenTheSimulatorIsDisabled_IsRejectedWithoutStoringAnything()
    {
        _options.FaultInjectionEnabled = false;

        var result = _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Equal("El simulador de fallas no está habilitado en este entorno.", result.ErrorMessage);
        Assert.Empty(_faults.Faults);
    }

    [Theory]
    [InlineData(SimulatedFaultTarget.Panel, SimulatedFaultType.InsulationFault)]
    [InlineData(SimulatedFaultTarget.Panel, SimulatedFaultType.InverterOverTemperature)]
    [InlineData(SimulatedFaultTarget.Inverter, SimulatedFaultType.OpenCircuit)]
    [InlineData(SimulatedFaultTarget.Inverter, SimulatedFaultType.SevereDegradation)]
    public void Inject_AFaultTypeThatDoesNotMatchTheEquipment_IsAValidationError(SimulatedFaultTarget target, SimulatedFaultType type)
    {
        var id = target == SimulatedFaultTarget.Panel ? _panel.Id : _inverter.Id;

        var result = _service.Inject(target, id, type, "admin");

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Empty(_faults.Faults);
    }

    [Fact]
    public void Inject_UnknownPanel_IsNotFound()
    {
        var result = _service.Inject(SimulatedFaultTarget.Panel, Guid.NewGuid(), SimulatedFaultType.OpenCircuit, "admin");

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El panel solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Inject_UnknownInverter_IsNotFound()
    {
        var result = _service.Inject(SimulatedFaultTarget.Inverter, Guid.NewGuid(), SimulatedFaultType.InsulationFault, "admin");

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El inversor solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Inject_PanelThatWasRemoved_IsAConflictBecauseItEmitsNoReadings()
    {
        var removed = AddPanel("PNL-002", _node, SolarPanelStatus.Inactive);

        var result = _service.Inject(SimulatedFaultTarget.Panel, removed.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Empty(_faults.Faults);
    }

    [Fact]
    public void Inject_PanelWithoutNode_IsAConflict()
    {
        var loose = AddPanel("PNL-003", null);

        var result = _service.Inject(SimulatedFaultTarget.Panel, loose.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public void Inject_PanelOfAnInactiveNode_IsAConflict()
    {
        _node.Status = SolarNodeStatus.Inactive;

        var result = _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public void Inject_InverterOfAnInactiveNode_IsAConflict()
    {
        _node.Status = SolarNodeStatus.Inactive;

        var result = _service.Inject(SimulatedFaultTarget.Inverter, _inverter.Id, SimulatedFaultType.InsulationFault, "admin");

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Empty(_faults.Faults);
    }

    [Fact]
    public void Inject_TheSameFaultTwice_IsAConflict()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        var result = _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("El equipo ya tiene esa falla activa.", result.ErrorMessage);
        Assert.Single(_faults.Faults);
    }

    [Fact]
    public void Inject_TwoDifferentFaultsInTheSameEquipment_AreBothStored()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.SevereDegradation, "admin");

        var result = _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.True(result.Success);
        Assert.Equal(2, _faults.Faults.Count);
    }

    [Fact]
    public void Inject_AfterClearingTheSameFault_CanBeInjectedAgain()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");
        _service.ClearTarget(SimulatedFaultTarget.Panel, _panel.Id);

        var result = _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        Assert.True(result.Success);
        Assert.Equal(2, _faults.Faults.Count);
    }

    // ---------- Restablecer un equipo ----------

    [Fact]
    public void ClearTarget_EndsAllTheActiveFaultsOfThatEquipmentOnly()
    {
        var other = AddPanel("PNL-002", _node);
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.SevereDegradation, "admin");
        _service.Inject(SimulatedFaultTarget.Panel, other.Id, SimulatedFaultType.OpenCircuit, "admin");

        var result = _service.ClearTarget(SimulatedFaultTarget.Panel, _panel.Id);

        Assert.True(result.Success);
        Assert.Equal("Falla simulada del panel \"PNL-001\" restablecida.", result.Value);
        Assert.All(_faults.Faults.Where(f => f.TargetId == _panel.Id), f => Assert.Equal(Now.UtcDateTime, f.EndedAtUtc));
        Assert.Null(_faults.Faults.Single(f => f.TargetId == other.Id).EndedAtUtc);
    }

    [Fact]
    public void ClearTarget_DoesNotDeleteAnything()
    {
        _service.Inject(SimulatedFaultTarget.Inverter, _inverter.Id, SimulatedFaultType.InsulationFault, "admin");

        _service.ClearTarget(SimulatedFaultTarget.Inverter, _inverter.Id);

        Assert.Single(_faults.Faults);
    }

    [Fact]
    public void ClearTarget_EquipmentWithoutFaults_IsNotAnError()
    {
        var result = _service.ClearTarget(SimulatedFaultTarget.Panel, _panel.Id);

        Assert.True(result.Success);
        Assert.Equal("El panel \"PNL-001\" no tenía fallas simuladas activas.", result.Value);
    }

    [Fact]
    public void ClearTarget_InverterMessage()
    {
        _service.Inject(SimulatedFaultTarget.Inverter, _inverter.Id, SimulatedFaultType.InverterOverTemperature, "admin");

        var result = _service.ClearTarget(SimulatedFaultTarget.Inverter, _inverter.Id);

        Assert.Equal("Falla simulada del inversor \"INV-001\" restablecida.", result.Value);
    }

    [Fact]
    public void ClearTarget_UnknownEquipment_IsNotFound()
    {
        Assert.Equal(ServiceErrorType.NotFound, _service.ClearTarget(SimulatedFaultTarget.Panel, Guid.NewGuid()).ErrorType);
        Assert.Equal(ServiceErrorType.NotFound, _service.ClearTarget(SimulatedFaultTarget.Inverter, Guid.NewGuid()).ErrorType);
    }

    [Fact]
    public void ClearTarget_WhenTheSimulatorIsDisabled_IsRejected()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");
        _options.FaultInjectionEnabled = false;

        var result = _service.ClearTarget(SimulatedFaultTarget.Panel, _panel.Id);

        Assert.Equal(ServiceErrorType.Unavailable, result.ErrorType);
        Assert.Null(_faults.Faults.Single().EndedAtUtc);
    }

    [Fact]
    public void ClearTarget_PanelThatWasRemovedWithAnActiveFault_CanStillBeCleared()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");
        _panel.Status = SolarPanelStatus.Inactive;

        var result = _service.ClearTarget(SimulatedFaultTarget.Panel, _panel.Id);

        Assert.True(result.Success);
        Assert.NotNull(_faults.Faults.Single().EndedAtUtc);
    }

    // ---------- Restablecer todas ----------

    [Fact]
    public void ClearAll_EndsEveryActiveFault()
    {
        var other = AddPanel("PNL-002", _node);
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");
        _service.Inject(SimulatedFaultTarget.Panel, other.Id, SimulatedFaultType.SevereDegradation, "admin");
        _service.Inject(SimulatedFaultTarget.Inverter, _inverter.Id, SimulatedFaultType.InsulationFault, "admin");

        var result = _service.ClearAll();

        Assert.True(result.Success);
        Assert.Equal("Todas las fallas simuladas fueron restablecidas.", result.Value);
        Assert.All(_faults.Faults, f => Assert.Equal(Now.UtcDateTime, f.EndedAtUtc));
        Assert.Equal(3, _faults.Faults.Count);
    }

    [Fact]
    public void ClearAll_WithoutActiveFaults_IsNotAnError()
    {
        var result = _service.ClearAll();

        Assert.True(result.Success);
        Assert.Equal("No había fallas simuladas activas.", result.Value);
    }

    [Fact]
    public void ClearAll_WhenTheSimulatorIsDisabled_IsRejected()
    {
        _options.FaultInjectionEnabled = false;

        Assert.Equal(ServiceErrorType.Unavailable, _service.ClearAll().ErrorType);
    }

    // ---------- Listado ----------

    [Fact]
    public void GetOverview_ListsTheEquipmentThatEmitsReadingsWithTheirEffectiveFault()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.SevereDegradation, "admin");
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");

        var overview = _service.GetOverview();

        Assert.True(overview.Enabled);
        var panel = Assert.Single(overview.Panels);
        Assert.Equal(_panel.Id, panel.Id);
        Assert.Equal("PNL-001", panel.SerieNumber);
        Assert.Equal("Parque Salto", panel.ParkName);
        Assert.Equal("Nodo Norte", panel.NodeName);
        Assert.Equal(SimulatedFaultType.OpenCircuit, panel.ActiveFault);
        Assert.Equal("Corte total (circuito abierto)", panel.ActiveFaultLabel);
        Assert.Equal(AlertSeverity.Critical, panel.ActiveFaultSeverity);

        var inverter = Assert.Single(overview.Inverters);
        Assert.Equal(_inverter.Id, inverter.Id);
        Assert.Equal("INV-001", inverter.SerieNumber);
        Assert.Equal("Nodo Norte", inverter.NodeName);
        Assert.Null(inverter.ActiveFault);
    }

    [Fact]
    public void GetOverview_LeavesOutWhatDoesNotEmitReadings()
    {
        AddPanel("PNL-REMOVED", _node, SolarPanelStatus.Inactive);
        AddPanel("PNL-LOOSE", null);
        var inactiveNode = new SolarNode { Id = Guid.NewGuid(), Name = "Nodo Sur", NormalizedName = "nodo sur", SolarParkId = _park.Id, SolarPark = _park, Status = SolarNodeStatus.Inactive };
        _nodes.PersistedNodes.Add(inactiveNode);
        AddPanel("PNL-INACTIVE-NODE", inactiveNode);
        _inverters.Inverters.Add(new Inverter { Id = Guid.NewGuid(), SerieNumber = "INV-002", NormalizedSerieNumber = "inv-002", SolarNodeId = inactiveNode.Id, SolarNode = inactiveNode });

        var overview = _service.GetOverview();

        Assert.Equal(["PNL-001"], overview.Panels.Select(p => p.SerieNumber));
        Assert.Equal(["INV-001"], overview.Inverters.Select(i => i.SerieNumber));
    }

    [Fact]
    public void GetOverview_ReportsWhetherTheSimulatorIsEnabled()
    {
        _options.FaultInjectionEnabled = false;

        Assert.False(_service.GetOverview().Enabled);
    }

    [Fact]
    public void GetOverview_AFaultThatWasClearedIsNoLongerShown()
    {
        _service.Inject(SimulatedFaultTarget.Panel, _panel.Id, SimulatedFaultType.OpenCircuit, "admin");
        _service.ClearTarget(SimulatedFaultTarget.Panel, _panel.Id);

        Assert.Null(_service.GetOverview().Panels.Single().ActiveFault);
    }
}
