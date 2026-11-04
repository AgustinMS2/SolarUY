using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarNodeListInverterTests
{
    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly FakeInverterRepository _inverterRepository = new();
    private readonly SolarNodeService _service;

    public SolarNodeListInverterTests()
    {
        _service = new SolarNodeService(_nodeRepository, new FakeSolarParkRepository(), _inverterRepository, NullLogger<SolarNodeService>.Instance);
    }

    private SolarNode AddNode(string name)
    {
        var node = new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = name,
            NormalizedName = name.ToLowerInvariant(),
            SolarParkId = Guid.NewGuid(),
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _nodeRepository.PersistedNodes.Add(node);
        return node;
    }

    private void AddInverter(SolarNode node, string serie)
    {
        _inverterRepository.Inverters.Add(new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            BrandAndModel = "Fronius Symo",
            NominalCapacityKw = 100,
            SolarNodeId = node.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    [Fact]
    public void GetAll_ShowsTheSerieOfTheAssignedInverterOrNone()
    {
        var withInverter = AddNode("Nodo A");
        AddNode("Nodo B");
        AddInverter(withInverter, "SN-001");

        var list = _service.GetAll();

        Assert.Equal("SN-001", list.Single(n => n.Name == "Nodo A").InverterSerie);
        Assert.Null(list.Single(n => n.Name == "Nodo B").InverterSerie);
    }

    [Fact]
    public void GetById_ShowsTheSerieOfTheAssignedInverter()
    {
        var node = AddNode("Nodo A");
        AddInverter(node, "SN-001");

        var result = _service.GetById(node.Id);

        Assert.True(result.Success);
        Assert.Equal("SN-001", result.Value!.InverterSerie);
    }
}
