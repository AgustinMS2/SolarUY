using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarNodeAssignInverterTests
{
    private const double ParkMaxCapacityKw = 5000;

    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly FakeSolarParkRepository _parkRepository = new();
    private readonly FakeInverterRepository _inverterRepository = new();
    private readonly SolarNodeService _service;
    private readonly SolarPark _park;
    private readonly SolarNode _node;

    public SolarNodeAssignInverterTests()
    {
        _service = new SolarNodeService(_nodeRepository, _parkRepository, _inverterRepository, new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);

        _park = new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Salto",
            NormalizedName = "parque salto",
            MaxCapacityKw = ParkMaxCapacityKw,
            PowerAlertThresholdKw = 4000,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-5)
        };
        _parkRepository.Parks.Add(_park);

        _node = AddNode("Nodo A");
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

    private Inverter AddInverter(SolarNode node, string serie, string brand = "Marca X1", double capacity = 100)
    {
        var inverter = new Inverter
        {
            Id = Guid.NewGuid(),
            SerieNumber = serie,
            NormalizedSerieNumber = serie.ToLowerInvariant(),
            BrandAndModel = brand,
            NominalCapacityKw = capacity,
            SolarNodeId = node.Id,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        _inverterRepository.Inverters.Add(inverter);
        return inverter;
    }

    private static AssignInverterRequest Request(
        string? serie = "SN-001",
        string? brand = "Fronius Symo",
        double capacity = 100,
        string? endpoint = null,
        bool confirmReplace = false)
    {
        return new AssignInverterRequest(serie, brand, capacity, endpoint, confirmReplace);
    }

    // ---- RF-1

    [Fact]
    public void AssignInverter_ValidInverterToActiveNode_LinksItAndUpdatesTheNodeDate()
    {
        var result = _service.AssignInverter(_node.Id, Request(serie: "  SN-001 ", brand: " Fronius Symo ", capacity: 150, endpoint: " 10.0.0.5 "));

        Assert.True(result.Success);
        var inverter = Assert.Single(_inverterRepository.Inverters);
        Assert.Equal(_node.Id, inverter.SolarNodeId);
        Assert.Equal("SN-001", inverter.SerieNumber);
        Assert.Equal("sn-001", inverter.NormalizedSerieNumber);
        Assert.Equal("Fronius Symo", inverter.BrandAndModel);
        Assert.Equal(150, inverter.NominalCapacityKw);
        Assert.Equal("10.0.0.5", inverter.Endpoint);
        Assert.NotNull(_node.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssignInverter_WithoutEndpoint_StoresNull(string? endpoint)
    {
        var result = _service.AssignInverter(_node.Id, Request(endpoint: endpoint));

        Assert.True(result.Success);
        Assert.Null(Assert.Single(_inverterRepository.Inverters).Endpoint);
    }

    // ---- RF-9, RF-6

    [Fact]
    public void AssignInverter_WhenNodeDoesNotExist_ReturnsNotFound()
    {
        var result = _service.AssignInverter(Guid.NewGuid(), Request());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
        Assert.Empty(_inverterRepository.Inverters);
    }

    [Fact]
    public void AssignInverter_WhenNodeIsInactive_ReturnsValidationAndPersistsNothing()
    {
        var inactive = AddNode("Nodo Inactivo", SolarNodeStatus.Inactive);

        var result = _service.AssignInverter(inactive.Id, Request());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("No se puede asignar un inversor a un nodo inactivo.", result.ErrorMessage);
        Assert.Empty(_inverterRepository.Inverters);
    }

    [Fact]
    public void AssignInverter_InactiveNodeIsReportedBeforeInvalidFields()
    {
        var inactive = AddNode("Nodo Inactivo", SolarNodeStatus.Inactive);

        var result = _service.AssignInverter(inactive.Id, Request(serie: "", capacity: -1));

        Assert.Equal("No se puede asignar un inversor a un nodo inactivo.", result.ErrorMessage);
    }

    // ---- RF-8

    [Theory]
    [InlineData(null, "Fronius", null, "El número de serie es obligatorio.")]
    [InlineData("", "Fronius", null, "El número de serie es obligatorio.")]
    [InlineData("   ", "Fronius", null, "El número de serie es obligatorio.")]
    [InlineData("SN-001", null, null, "La marca y modelo son obligatorios.")]
    [InlineData("SN-001", "   ", null, "La marca y modelo son obligatorios.")]
    public void AssignInverter_WithMissingTextData_ReturnsValidation(string? serie, string? brand, string? endpoint, string expectedMessage)
    {
        var result = _service.AssignInverter(_node.Id, Request(serie: serie, brand: brand, endpoint: endpoint));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(expectedMessage, result.ErrorMessage);
        Assert.Empty(_inverterRepository.Inverters);
    }

    [Fact]
    public void AssignInverter_WithSerieLongerThan50_ReturnsValidation()
    {
        var result = _service.AssignInverter(_node.Id, Request(serie: new string('S', 51)));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El número de serie no puede superar los 50 caracteres.", result.ErrorMessage);
    }

    [Fact]
    public void AssignInverter_WithSerieOf50Characters_IsAccepted()
    {
        var result = _service.AssignInverter(_node.Id, Request(serie: new string('S', 50)));

        Assert.True(result.Success);
    }

    [Fact]
    public void AssignInverter_WithBrandAndModelLongerThan100_ReturnsValidation()
    {
        var result = _service.AssignInverter(_node.Id, Request(brand: new string('M', 101)));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La marca y modelo no pueden superar los 100 caracteres.", result.ErrorMessage);
    }

    [Fact]
    public void AssignInverter_WithEndpointLongerThan100_ReturnsValidation()
    {
        var result = _service.AssignInverter(_node.Id, Request(endpoint: new string('e', 101)));

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La dirección o endpoint no puede superar los 100 caracteres.", result.ErrorMessage);
    }

    // ---- RF-4, RF-5

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AssignInverter_WithCapacityNotGreaterThanZero_ReturnsValidation(double capacity)
    {
        var result = _service.AssignInverter(_node.Id, Request(capacity: capacity));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La capacidad nominal del inversor debe ser mayor a 0.", result.ErrorMessage);
        Assert.Empty(_inverterRepository.Inverters);
    }

    [Fact]
    public void AssignInverter_WithCapacityAboveTheParkMaximum_ReturnsValidation()
    {
        var result = _service.AssignInverter(_node.Id, Request(capacity: ParkMaxCapacityKw + 0.1));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La capacidad nominal del inversor no puede superar la capacidad máxima del parque solar.", result.ErrorMessage);
        Assert.Empty(_inverterRepository.Inverters);
    }

    [Fact]
    public void AssignInverter_WithCapacityEqualToTheParkMaximum_IsAccepted()
    {
        var result = _service.AssignInverter(_node.Id, Request(capacity: ParkMaxCapacityKw));

        Assert.True(result.Success);
    }

    // ---- RF-3

    [Theory]
    [InlineData("SN-OTHER")]
    [InlineData("  sn-other  ")]
    public void AssignInverter_WhenSerieBelongsToAnotherNode_ReturnsConflictNamingThatNode(string requestedSerie)
    {
        var otherNode = AddNode("Nodo B");
        AddInverter(otherNode, "SN-OTHER");

        var result = _service.AssignInverter(_node.Id, Request(serie: requestedSerie));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal($"El inversor con número de serie \"{requestedSerie.Trim()}\" ya se encuentra asignado al nodo \"Nodo B\".", result.ErrorMessage);
        Assert.Single(_inverterRepository.Inverters);
        Assert.Null(_node.UpdatedAtUtc);
    }

    // ---- RF-2

    [Fact]
    public void AssignInverter_WhenNodeHasAnotherInverterAndNotConfirmed_AsksForConfirmationAndChangesNothing()
    {
        var current = AddInverter(_node, "SN-OLD", brand: "Marca vieja");

        var result = _service.AssignInverter(_node.Id, Request(serie: "SN-NEW"));

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.ConfirmationRequired, result.ErrorType);
        Assert.Equal("El nodo ya tiene el inversor \"SN-OLD\". ¿Deseás reemplazarlo por \"SN-NEW\"?", result.ErrorMessage);
        var unchanged = Assert.Single(_inverterRepository.Inverters);
        Assert.Equal("SN-OLD", unchanged.SerieNumber);
        Assert.Equal("Marca vieja", unchanged.BrandAndModel);
        Assert.Equal(current.Id, unchanged.Id);
        Assert.Null(_node.UpdatedAtUtc);
    }

    [Fact]
    public void AssignInverter_WhenReplaceIsConfirmed_ReplacesTheInverterKeepingASingleOne()
    {
        AddInverter(_node, "SN-OLD");

        var result = _service.AssignInverter(_node.Id, Request(serie: "SN-NEW", brand: "Marca nueva", capacity: 300, confirmReplace: true));

        Assert.True(result.Success);
        var replaced = Assert.Single(_inverterRepository.Inverters);
        Assert.Equal("SN-NEW", replaced.SerieNumber);
        Assert.Equal("sn-new", replaced.NormalizedSerieNumber);
        Assert.Equal("Marca nueva", replaced.BrandAndModel);
        Assert.Equal(300, replaced.NominalCapacityKw);
        Assert.Equal(_node.Id, replaced.SolarNodeId);
        Assert.NotNull(_node.UpdatedAtUtc);
        Assert.Null(_inverterRepository.GetBySerie("sn-old"));
    }

    // ---- RF-10

    [Fact]
    public void AssignInverter_WithTheSameSerieAlreadyInTheNode_UpdatesItsDataWithoutAskingConfirmation()
    {
        var current = AddInverter(_node, "SN-001", brand: "Marca vieja", capacity: 100);

        var result = _service.AssignInverter(_node.Id, Request(serie: " sn-001 ", brand: "Marca corregida", capacity: 250, endpoint: "192.168.1.10"));

        Assert.True(result.Success);
        var updated = Assert.Single(_inverterRepository.Inverters);
        Assert.Equal(current.Id, updated.Id);
        Assert.Equal("Marca corregida", updated.BrandAndModel);
        Assert.Equal(250, updated.NominalCapacityKw);
        Assert.Equal("192.168.1.10", updated.Endpoint);
        Assert.NotNull(_node.UpdatedAtUtc);
    }
}
