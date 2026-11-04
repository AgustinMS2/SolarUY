using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

/// <summary>Spec 014 RF-9: the node list shows the real panel count and the panel list shows each panel's node.</summary>
public class SolarPanelNodeListingTests
{
    private readonly FakeSolarPanelRepository _panelRepository = new();
    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly SolarNodeService _nodeService;
    private readonly SolarPanelService _panelService;
    private readonly Guid _parkId = Guid.NewGuid();

    public SolarPanelNodeListingTests()
    {
        var inverters = new FakeInverterRepository();
        _nodeService = new SolarNodeService(_nodeRepository, new FakeSolarParkRepository(), inverters, _panelRepository, NullLogger<SolarNodeService>.Instance);
        _panelService = new SolarPanelService(_panelRepository, _nodeRepository, inverters, NullLogger<SolarPanelService>.Instance);
    }

    private SolarNode AddNode(string name)
    {
        var node = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            SolarParkId = _parkId,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _nodeRepository.PersistedNodes.Add(node);
        return node;
    }

    private SolarPanel AddPanel(string serie, SolarNode? node, SolarPanelStatus status = SolarPanelStatus.Active)
    {
        var panel = new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            PowerW = 500,
            Efficiency = 20,
            SolarParkId = _parkId,
            SolarNodeId = node?.Id,
            SolarNode = node,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
        _panelRepository.Panels.Add(panel);
        return panel;
    }

    // ---- Node list: real panel count

    [Fact]
    public void NodeGetAll_ShowsTheNumberOfPanelsAssociatedToEachNode()
    {
        var nodeA = AddNode("Nodo A");
        var nodeB = AddNode("Nodo B");
        AddPanel("P-1", nodeA);
        AddPanel("P-2", nodeA);
        AddPanel("P-3", nodeB);
        AddPanel("P-4", node: null);

        var list = _nodeService.GetAll();

        Assert.Equal(2, list.Single(n => n.Name == "Nodo A").PanelsCount);
        Assert.Equal(1, list.Single(n => n.Name == "Nodo B").PanelsCount);
    }

    [Fact]
    public void NodeGetAll_ShowsZeroForNodesWithoutPanels()
    {
        AddNode("Nodo vacío");

        var list = _nodeService.GetAll();

        Assert.Equal(0, Assert.Single(list).PanelsCount);
    }

    [Fact]
    public void NodeGetAll_CountsInactivePanelsThatKeepTheirAssociation()
    {
        var node = AddNode("Nodo A");
        AddPanel("P-1", node);
        AddPanel("P-2", node, SolarPanelStatus.Inactive);

        var list = _nodeService.GetAll();

        Assert.Equal(2, Assert.Single(list).PanelsCount);
    }

    [Fact]
    public void NodeGetById_ShowsTheNumberOfPanels()
    {
        var node = AddNode("Nodo A");
        AddPanel("P-1", node);
        AddPanel("P-2", node);

        var result = _nodeService.GetById(node.Id);

        Assert.True(result.Success);
        Assert.Equal(2, result.Value!.PanelsCount);
    }

    // ---- Panel list: node of each panel

    [Fact]
    public void PanelGetAll_ShowsTheNodeOfEachPanelOrNoneWhenUnassigned()
    {
        var node = AddNode("Nodo A");
        AddPanel("P-1", node);
        AddPanel("P-2", node: null);

        var list = _panelService.GetAll();

        var assigned = list.Single(p => p.SerieNumber == "P-1");
        var available = list.Single(p => p.SerieNumber == "P-2");
        Assert.Equal("Nodo A", assigned.SolarNodeName);
        Assert.Equal(node.Id, assigned.SolarNodeId);
        Assert.Null(available.SolarNodeName);
        Assert.Null(available.SolarNodeId);
    }

    [Fact]
    public void PanelGetById_ShowsTheNodeOfThePanel()
    {
        var node = AddNode("Nodo A");
        var panel = AddPanel("P-1", node);

        var result = _panelService.GetById(panel.Id);

        Assert.True(result.Success);
        Assert.Equal("Nodo A", result.Value!.SolarNodeName);
        Assert.Equal(node.Id, result.Value.SolarNodeId);
    }
}
