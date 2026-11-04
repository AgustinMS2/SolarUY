using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarPanelAssignNodeTests
{
    private const string InactiveMessage = "No se pueden asociar paneles inactivos o a nodos inactivos.";

    private readonly FakeSolarPanelRepository _panelRepository = new();
    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly FakeInverterRepository _inverterRepository = new();
    private readonly SolarPanelService _service;
    private readonly Guid _parkId = Guid.NewGuid();
    private readonly Guid _otherParkId = Guid.NewGuid();
    private readonly SolarNode _node;

    public SolarPanelAssignNodeTests()
    {
        _service = new SolarPanelService(_panelRepository, _nodeRepository, _inverterRepository, NullLogger<SolarPanelService>.Instance);
        _node = AddNode("Nodo A");
    }

    private SolarNode AddNode(string name, SolarNodeStatus status = SolarNodeStatus.Active, Guid? parkId = null)
    {
        var node = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            SolarParkId = parkId ?? _parkId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
        };
        _nodeRepository.PersistedNodes.Add(node);
        return node;
    }

    private SolarPanel AddPanel(string serie, int powerW = 500, SolarPanelStatus status = SolarPanelStatus.Active, Guid? parkId = null, Guid? nodeId = null)
    {
        var panel = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            PowerW = powerW,
            Efficiency = 20,
            SolarParkId = parkId ?? _parkId,
            SolarNodeId = nodeId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        _panelRepository.Panels.Add(panel);
        return panel;
    }

    private void AddInverter(SolarNode node, double capacityKw)
    {
        _inverterRepository.Inverters.Add(new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = "INV-1",
            NormalizedSerieNumber = "inv-1",
            BrandAndModel = "Fronius",
            NominalCapacityKw = capacityKw,
            SolarNodeId = node.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static AssignPanelsRequest Request(IEnumerable<SolarPanel> panels, bool confirm = false)
    {
        return new AssignPanelsRequest(panels.Select(p => p.Id).ToList(), confirm);
    }

    // ---- RF-1

    [Fact]
    public void AssignPanelsToNode_AvailablePanelsOfTheSamePark_AssignsTheWholeBatch()
    {
        var panels = new[] { AddPanel("P-1"), AddPanel("P-2"), AddPanel("P-3") };

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.True(result.Success);
        Assert.All(panels, p =>
        {
            Assert.Equal(_node.Id, p.SolarNodeId);
            Assert.NotNull(p.UpdatedAtUtc);
        });
    }

    [Fact]
    public void AssignPanelsToNode_PanelsAlreadyInTheNode_AreIgnoredWithoutChangingThem()
    {
        var alreadyThere = AddPanel("P-1", nodeId: _node.Id);
        var available = AddPanel("P-2");

        var result = _service.AssignPanelsToNode(_node.Id, Request([alreadyThere, available]));

        Assert.True(result.Success);
        Assert.Null(alreadyThere.UpdatedAtUtc);
        Assert.Equal(_node.Id, available.SolarNodeId);
    }

    [Fact]
    public void AssignPanelsToNode_WhenEveryPanelIsAlreadyInTheNode_SucceedsWithoutChanges()
    {
        var alreadyThere = AddPanel("P-1", nodeId: _node.Id);

        var result = _service.AssignPanelsToNode(_node.Id, Request([alreadyThere]));

        Assert.True(result.Success);
        Assert.Null(alreadyThere.UpdatedAtUtc);
    }

    // ---- RF-7

    [Theory]
    [MemberData(nameof(EmptySelections))]
    public void AssignPanelsToNode_WithoutPanels_ReturnsValidation(IReadOnlyList<Guid>? ids)
    {
        var result = _service.AssignPanelsToNode(_node.Id, new AssignPanelsRequest(ids, false));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Seleccioná al menos un panel.", result.ErrorMessage);
    }

    public static IEnumerable<object?[]> EmptySelections()
    {
        yield return [null];
        yield return [new List<Guid>()];
    }

    [Fact]
    public void AssignPanelsToNode_WithMoreThan50Panels_ReturnsValidationAndChangesNothing()
    {
        var panels = Enumerable.Range(1, 51).Select(i => AddPanel($"P-{i}")).ToList();

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Se pueden asignar hasta 50 paneles a la vez.", result.ErrorMessage);
        Assert.All(panels, p => Assert.Null(p.SolarNodeId));
    }

    [Fact]
    public void AssignPanelsToNode_WithExactly50Panels_IsAccepted()
    {
        var panels = Enumerable.Range(1, 50).Select(i => AddPanel($"P-{i}")).ToList();

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.True(result.Success);
        Assert.All(panels, p => Assert.Equal(_node.Id, p.SolarNodeId));
    }

    [Fact]
    public void AssignPanelsToNode_RepeatedIdsCountOnlyOnce()
    {
        var panels = Enumerable.Range(1, 50).Select(i => AddPanel($"P-{i}")).ToList();
        var ids = panels.Concat(panels).Select(p => p.Id).ToList();

        var result = _service.AssignPanelsToNode(_node.Id, new AssignPanelsRequest(ids, false));

        Assert.True(result.Success);
    }

    // ---- RF-8, RF-4

    [Fact]
    public void AssignPanelsToNode_WhenNodeDoesNotExist_ReturnsNotFoundBeforeValidatingTheBatch()
    {
        var result = _service.AssignPanelsToNode(Guid.NewGuid(), new AssignPanelsRequest([], false));

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void AssignPanelsToNode_WhenPanelDoesNotExist_ReturnsNotFoundAndChangesNothing()
    {
        var valid = AddPanel("P-1");

        var result = _service.AssignPanelsToNode(_node.Id, new AssignPanelsRequest([valid.Id, Guid.NewGuid()], false));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Uno o más paneles seleccionados no existen.", result.ErrorMessage);
        Assert.Null(valid.SolarNodeId);
    }

    [Fact]
    public void AssignPanelsToNode_WhenNodeIsInactive_ReturnsValidationAndChangesNothing()
    {
        var inactiveNode = AddNode("Nodo Inactivo", SolarNodeStatus.Inactive);
        var panel = AddPanel("P-1");

        var result = _service.AssignPanelsToNode(inactiveNode.Id, Request([panel]));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(InactiveMessage, result.ErrorMessage);
        Assert.Null(panel.SolarNodeId);
    }

    [Fact]
    public void AssignPanelsToNode_InactiveNodeIsReportedBeforeMissingPanels()
    {
        var inactiveNode = AddNode("Nodo Inactivo", SolarNodeStatus.Inactive);

        var result = _service.AssignPanelsToNode(inactiveNode.Id, new AssignPanelsRequest([Guid.NewGuid()], false));

        Assert.Equal(InactiveMessage, result.ErrorMessage);
    }

    [Fact]
    public void AssignPanelsToNode_WhenAnyPanelIsInactive_RejectsTheWholeBatch()
    {
        var active = AddPanel("P-1");
        var inactive = AddPanel("P-2", status: SolarPanelStatus.Inactive);

        var result = _service.AssignPanelsToNode(_node.Id, Request([active, inactive]));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(InactiveMessage, result.ErrorMessage);
        Assert.Null(active.SolarNodeId);
        Assert.Null(inactive.SolarNodeId);
    }

    // ---- RF-2

    [Fact]
    public void AssignPanelsToNode_WhenAnyPanelBelongsToAnotherPark_RejectsTheWholeBatch()
    {
        var sameParkPanel = AddPanel("P-1");
        var otherParkPanel = AddPanel("P-2", parkId: _otherParkId);

        var result = _service.AssignPanelsToNode(_node.Id, Request([sameParkPanel, otherParkPanel]));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("No se pueden asociar paneles de otro parque.", result.ErrorMessage);
        Assert.Null(sameParkPanel.SolarNodeId);
        Assert.Null(otherParkPanel.SolarNodeId);
    }

    // ---- RF-3

    [Fact]
    public void AssignPanelsToNode_WhenPanelBelongsToAnotherNode_AsksForConfirmationAndChangesNothing()
    {
        var origin = AddNode("Nodo B");
        var panel = AddPanel("SN-77", nodeId: origin.Id);

        var result = _service.AssignPanelsToNode(_node.Id, Request([panel]));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        Assert.Equal("El panel \"SN-77\" ya pertenece al nodo \"Nodo B\". ¿Deseás reasignarlo a este nodo?", result.ErrorMessage);
        Assert.Equal(origin.Id, panel.SolarNodeId);
    }

    [Theory]
    [InlineData(2, "Se reasignarán también 1 panel más.")]
    [InlineData(3, "Se reasignarán también 2 paneles más.")]
    public void AssignPanelsToNode_WithSeveralPanelsInOtherNodes_MentionsTheFirstAndCountsTheRest(int movedPanels, string expectedSuffix)
    {
        var origin = AddNode("Nodo B");
        var panels = Enumerable.Range(1, movedPanels).Select(i => AddPanel($"SN-{i}", nodeId: origin.Id)).ToList();

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        Assert.StartsWith("El panel \"SN-1\" ya pertenece al nodo \"Nodo B\". ¿Deseás reasignarlo a este nodo?", result.ErrorMessage);
        Assert.EndsWith(expectedSuffix, result.ErrorMessage);
    }

    [Fact]
    public void AssignPanelsToNode_WhenReassignmentIsConfirmed_MovesThePanelsToTheNewNode()
    {
        var origin = AddNode("Nodo B");
        var moved = AddPanel("SN-1", nodeId: origin.Id);
        var available = AddPanel("SN-2");

        var result = _service.AssignPanelsToNode(_node.Id, Request([moved, available], confirm: true));

        Assert.True(result.Success);
        Assert.Equal(_node.Id, moved.SolarNodeId);
        Assert.Equal(_node.Id, available.SolarNodeId);
    }

    // ---- RF-6

    [Fact]
    public void AssignPanelsToNode_WhenPowerExceedsTheInverterCapacity_AsksForConfirmation()
    {
        AddInverter(_node, capacityKw: 1);
        var panels = new[] { AddPanel("P-1", 600), AddPanel("P-2", 600) };

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        Assert.Equal("La potencia acumulada de los paneles (1.2 kW) supera la capacidad nominal del inversor (1 kW).", result.ErrorMessage);
        Assert.All(panels, p => Assert.Null(p.SolarNodeId));
    }

    [Fact]
    public void AssignPanelsToNode_WhenPowerWarningIsConfirmed_AssignsThePanels()
    {
        AddInverter(_node, capacityKw: 1);
        var panels = new[] { AddPanel("P-1", 600), AddPanel("P-2", 600) };

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels, confirm: true));

        Assert.True(result.Success);
        Assert.All(panels, p => Assert.Equal(_node.Id, p.SolarNodeId));
    }

    [Fact]
    public void AssignPanelsToNode_WhenPowerEqualsTheInverterCapacity_DoesNotWarn()
    {
        AddInverter(_node, capacityKw: 1);
        var panels = new[] { AddPanel("P-1", 500), AddPanel("P-2", 500) };

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.True(result.Success);
    }

    [Fact]
    public void AssignPanelsToNode_WhenTheNodeHasNoInverter_NeverWarnsAboutPower()
    {
        var panels = new[] { AddPanel("P-1", 100000) };

        var result = _service.AssignPanelsToNode(_node.Id, Request(panels));

        Assert.True(result.Success);
    }

    [Fact]
    public void AssignPanelsToNode_PowerWarningCountsThePanelsAlreadyInTheNode()
    {
        AddInverter(_node, capacityKw: 1);
        AddPanel("P-OLD", 800, nodeId: _node.Id);
        var newPanel = AddPanel("P-NEW", 300);

        var result = _service.AssignPanelsToNode(_node.Id, Request([newPanel]));

        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        Assert.Equal("La potencia acumulada de los paneles (1.1 kW) supera la capacidad nominal del inversor (1 kW).", result.ErrorMessage);
    }

    [Fact]
    public void AssignPanelsToNode_PowerWarningIgnoresInactivePanelsAlreadyInTheNode()
    {
        AddInverter(_node, capacityKw: 1);
        AddPanel("P-OLD", 900, status: SolarPanelStatus.Inactive, nodeId: _node.Id);
        var newPanel = AddPanel("P-NEW", 500);

        var result = _service.AssignPanelsToNode(_node.Id, Request([newPanel]));

        Assert.True(result.Success);
    }

    [Fact]
    public void AssignPanelsToNode_ReassignmentAndPowerWarningsAreShownTogether()
    {
        AddInverter(_node, capacityKw: 1);
        var origin = AddNode("Nodo B");
        var moved = AddPanel("SN-1", 700, nodeId: origin.Id);
        var available = AddPanel("SN-2", 700);

        var result = _service.AssignPanelsToNode(_node.Id, Request([moved, available]));

        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        var lines = result.ErrorMessage!.Split('\n');
        Assert.Equal(2, lines.Length);
        Assert.StartsWith("El panel \"SN-1\" ya pertenece al nodo \"Nodo B\".", lines[0]);
        Assert.Equal("La potencia acumulada de los paneles (1.4 kW) supera la capacidad nominal del inversor (1 kW).", lines[1]);
    }
}
