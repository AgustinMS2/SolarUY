using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarPanelUnassignNodeTests
{
    private readonly FakeSolarPanelRepository _panelRepository = new();
    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly SolarPanelService _service;
    private readonly Guid _parkId = Guid.NewGuid();
    private readonly SolarNode _node;

    public SolarPanelUnassignNodeTests()
    {
        _service = new SolarPanelService(_panelRepository, _nodeRepository, new FakeInverterRepository(), NullLogger<SolarPanelService>.Instance);
        _node = AddNode("Nodo A");
    }

    private SolarNode AddNode(string name, SolarNodeStatus status = SolarNodeStatus.Active)
    {
        var node = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            SolarParkId = _parkId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-3)
        };
        _nodeRepository.PersistedNodes.Add(node);
        return node;
    }

    private SolarPanel AddPanel(string serie, Guid? nodeId, SolarPanelStatus status = SolarPanelStatus.Active)
    {
        var panel = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            PowerW = 500,
            Efficiency = 20,
            SolarParkId = _parkId,
            SolarNodeId = nodeId,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        _panelRepository.Panels.Add(panel);
        return panel;
    }

    private static UnassignPanelsRequest Request(params SolarPanel[] panels)
    {
        return new UnassignPanelsRequest(panels.Select(p => p.Id).ToList());
    }

    [Fact]
    public void UnassignPanelsFromNode_PanelsOfTheNode_LeavesThemWithoutNode()
    {
        var first = AddPanel("P-1", _node.Id);
        var second = AddPanel("P-2", _node.Id);
        var untouched = AddPanel("P-3", _node.Id);

        var result = _service.UnassignPanelsFromNode(_node.Id, Request(first, second));

        Assert.True(result.Success);
        Assert.Null(first.SolarNodeId);
        Assert.Null(second.SolarNodeId);
        Assert.NotNull(first.UpdatedAtUtc);
        Assert.Equal(_node.Id, untouched.SolarNodeId);
        Assert.Null(untouched.UpdatedAtUtc);
    }

    [Fact]
    public void UnassignPanelsFromNode_WhenAPanelBelongsToAnotherNode_RejectsTheWholeBatch()
    {
        var otherNode = AddNode("Nodo B");
        var mine = AddPanel("P-1", _node.Id);
        var foreign = AddPanel("SN-9", otherNode.Id);

        var result = _service.UnassignPanelsFromNode(_node.Id, Request(mine, foreign));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El panel \"SN-9\" no está asociado a este nodo.", result.ErrorMessage);
        Assert.Equal(_node.Id, mine.SolarNodeId);
        Assert.Equal(otherNode.Id, foreign.SolarNodeId);
    }

    [Fact]
    public void UnassignPanelsFromNode_WhenAPanelHasNoNode_RejectsIt()
    {
        var available = AddPanel("SN-5", nodeId: null);

        var result = _service.UnassignPanelsFromNode(_node.Id, Request(available));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El panel \"SN-5\" no está asociado a este nodo.", result.ErrorMessage);
    }

    [Fact]
    public void UnassignPanelsFromNode_WhenNodeDoesNotExist_ReturnsNotFound()
    {
        var result = _service.UnassignPanelsFromNode(Guid.NewGuid(), new UnassignPanelsRequest([Guid.NewGuid()]));

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void UnassignPanelsFromNode_WhenPanelDoesNotExist_ReturnsNotFoundAndChangesNothing()
    {
        var mine = AddPanel("P-1", _node.Id);

        var result = _service.UnassignPanelsFromNode(_node.Id, new UnassignPanelsRequest([mine.Id, Guid.NewGuid()]));

        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("Uno o más paneles seleccionados no existen.", result.ErrorMessage);
        Assert.Equal(_node.Id, mine.SolarNodeId);
    }

    [Fact]
    public void UnassignPanelsFromNode_WithoutPanels_ReturnsValidation()
    {
        var result = _service.UnassignPanelsFromNode(_node.Id, new UnassignPanelsRequest(null));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Seleccioná al menos un panel.", result.ErrorMessage);
    }

    [Fact]
    public void UnassignPanelsFromNode_WithMoreThan50Panels_ReturnsValidationAndChangesNothing()
    {
        var panels = Enumerable.Range(1, 51).Select(i => AddPanel($"P-{i}", _node.Id)).ToArray();

        var result = _service.UnassignPanelsFromNode(_node.Id, Request(panels));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("Se pueden asignar hasta 50 paneles a la vez.", result.ErrorMessage);
        Assert.All(panels, p => Assert.Equal(_node.Id, p.SolarNodeId));
    }

    [Fact]
    public void UnassignPanelsFromNode_RepeatedIdsCountOnlyOnce()
    {
        var panel = AddPanel("P-1", _node.Id);

        var result = _service.UnassignPanelsFromNode(_node.Id, new UnassignPanelsRequest([panel.Id, panel.Id]));

        Assert.True(result.Success);
        Assert.Null(panel.SolarNodeId);
    }

    [Fact]
    public void UnassignPanelsFromNode_IsAllowedOnAnInactiveNode()
    {
        var inactiveNode = AddNode("Nodo Inactivo", SolarNodeStatus.Inactive);
        var panel = AddPanel("P-1", inactiveNode.Id);

        var result = _service.UnassignPanelsFromNode(inactiveNode.Id, Request(panel));

        Assert.True(result.Success);
        Assert.Null(panel.SolarNodeId);
    }

    [Fact]
    public void UnassignPanelsFromNode_IsAllowedForInactivePanels()
    {
        var panel = AddPanel("P-1", _node.Id, SolarPanelStatus.Inactive);

        var result = _service.UnassignPanelsFromNode(_node.Id, Request(panel));

        Assert.True(result.Success);
        Assert.Null(panel.SolarNodeId);
    }
}
