using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarNodeUnassignInverterTests
{
    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly FakeSolarParkRepository _parkRepository = new();
    private readonly FakeInverterRepository _inverterRepository = new();
    private readonly SolarNodeService _service;
    private readonly SolarPark _park;

    public SolarNodeUnassignInverterTests()
    {
        _service = new SolarNodeService(_nodeRepository, _parkRepository, _inverterRepository, new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);

        _park = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Salto",
            NormalizedName = "parque salto",
            MaxCapacityKw = 5000,
            PowerAlertThresholdKw = 4000,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        _parkRepository.Parks.Add(_park);
    }

    private SolarNode AddNode(string name, SolarNodeStatus status = SolarNodeStatus.Active)
    {
        var node = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            SolarParkId = _park.Id,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };
        _nodeRepository.PersistedNodes.Add(node);
        return node;
    }

    private Inverter AddInverter(SolarNode node, string serie)
    {
        var inverter = new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            BrandAndModel = "Fronius Symo",
            NominalCapacityKw = 100,
            SolarNodeId = node.Id,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        _inverterRepository.Inverters.Add(inverter);
        return inverter;
    }

    [Fact]
    public void UnassignInverter_NodeWithInverter_RemovesItAndUpdatesTheNodeDate()
    {
        var node = AddNode("Nodo A");
        AddInverter(node, "SN-001");

        var result = _service.UnassignInverter(node.Id);

        Assert.True(result.Success);
        Assert.Empty(_inverterRepository.Inverters);
        Assert.NotNull(node.UpdatedAtUtc);
    }

    [Fact]
    public void UnassignInverter_OnlyAffectsTheInverterOfThatNode()
    {
        var nodeA = AddNode("Nodo A");
        var nodeB = AddNode("Nodo B");
        AddInverter(nodeA, "SN-A");
        var inverterB = AddInverter(nodeB, "SN-B");

        var result = _service.UnassignInverter(nodeA.Id);

        Assert.True(result.Success);
        Assert.Equal(inverterB.Id, Assert.Single(_inverterRepository.Inverters).Id);
        Assert.Null(nodeB.UpdatedAtUtc);
    }

    [Fact]
    public void UnassignInverter_NodeWithoutInverter_ReturnsNotFound()
    {
        var node = AddNode("Nodo A");

        var result = _service.UnassignInverter(node.Id);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo no tiene un inversor asignado.", result.ErrorMessage);
        Assert.Null(node.UpdatedAtUtc);
    }

    [Fact]
    public void UnassignInverter_NodeDoesNotExist_ReturnsNotFound()
    {
        var result = _service.UnassignInverter(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void UnassignInverter_InactiveNode_IsAllowedSoTheInverterCanBeFreed()
    {
        var node = AddNode("Nodo Inactivo", SolarNodeStatus.Inactive);
        AddInverter(node, "SN-001");

        var result = _service.UnassignInverter(node.Id);

        Assert.True(result.Success);
        Assert.Empty(_inverterRepository.Inverters);
    }

    [Fact]
    public void UnassignInverter_FreesTheSerieSoItCanBeAssignedToAnotherNode()
    {
        var nodeA = AddNode("Nodo A");
        var nodeB = AddNode("Nodo B");
        AddInverter(nodeA, "SN-001");
        _service.UnassignInverter(nodeA.Id);

        var result = _service.AssignInverter(nodeB.Id, new AssignInverterRequest("SN-001", "Fronius Symo", 100, null, false));

        Assert.True(result.Success);
        Assert.Equal(nodeB.Id, Assert.Single(_inverterRepository.Inverters).SolarNodeId);
    }
}
