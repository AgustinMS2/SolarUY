using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarNodeServiceUpdateTests
{
    private readonly FakeSolarNodeRepository _nodeRepository = new();
    private readonly FakeSolarParkRepository _parkRepository = new();
    private readonly SolarNodeService _service;
    private readonly Guid _parkId = Guid.NewGuid();
    private readonly Guid _anotherParkId = Guid.NewGuid();
    private readonly Guid _nodeId = Guid.NewGuid();
    private readonly DateTime _initialCreatedUtc = DateTime.UtcNow.AddDays(-10);

    public SolarNodeServiceUpdateTests()
    {
        _service = new SolarNodeService(_nodeRepository, _parkRepository, new FakeInverterRepository(), NullLogger<SolarNodeService>.Instance);

        var park = new SolarPark
        {
            Id = _parkId,
            Name = "Parque Salto Norte",
            NormalizedName = "parque salto norte",
            Status = SolarParkStatus.Active
        };

        _parkRepository.Parks.Add(park);
        _parkRepository.Parks.Add(new SolarPark
        {
            Id = _anotherParkId,
            Name = "Parque Paysandú",
            NormalizedName = "parque paysandú",
            Status = SolarParkStatus.Active
        });

        _nodeRepository.PersistedNodes.Add(new SolarNode
        {
            Id = _nodeId,
            Name = "Nodo Original",
            NormalizedName = "nodo original",
            SolarParkId = _parkId,
            SolarPark = park,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = _initialCreatedUtc,
            UpdatedAtUtc = null
        });
    }

    [Fact]
    public void Update_WithValidName_ReturnsSuccessAndUpdatesNameAndModifiedDate()
    {
        var request = new UpdateSolarNodeRequest("  Nodo Renombrado  ");

        var result = _service.Update(_nodeId, request);

        Assert.True(result.Success);
        var updated = _nodeRepository.GetById(_nodeId);
        Assert.NotNull(updated);
        Assert.Equal("Nodo Renombrado", updated.Name);
        Assert.Equal("nodo renombrado", updated.NormalizedName);
        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.True(updated.UpdatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Update_PreservesParkStatusAndCreatedAt()
    {
        var request = new UpdateSolarNodeRequest("Nodo Renombrado");

        var result = _service.Update(_nodeId, request);

        Assert.True(result.Success);
        var updated = _nodeRepository.GetById(_nodeId);
        Assert.NotNull(updated);
        Assert.Equal(_parkId, updated.SolarParkId);
        Assert.Equal(SolarNodeStatus.Active, updated.Status);
        Assert.Equal(_initialCreatedUtc, updated.CreatedAtUtc);
    }

    [Fact]
    public void Update_WhenNodeDoesNotExist_ReturnsNotFound()
    {
        var request = new UpdateSolarNodeRequest("Nodo Renombrado");

        var result = _service.Update(Guid.NewGuid(), request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenNameIsEmptyOrWhitespace_ReturnsValidationError(string? invalidName)
    {
        var request = new UpdateSolarNodeRequest(invalidName!);

        var result = _service.Update(_nodeId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre es obligatorio.", result.ErrorMessage);

        var unchanged = _nodeRepository.GetById(_nodeId);
        Assert.NotNull(unchanged);
        Assert.Equal("Nodo Original", unchanged.Name);
    }

    [Fact]
    public void Update_WhenNameExceeds100Characters_ReturnsValidationError()
    {
        var request = new UpdateSolarNodeRequest(new string('A', 101));

        var result = _service.Update(_nodeId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre no puede superar los 100 caracteres.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenNameIsUsedByAnotherNodeInSamePark_ReturnsConflict()
    {
        _nodeRepository.PersistedNodes.Add(new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo Existente",
            NormalizedName = "nodo existente",
            SolarParkId = _parkId,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        var request = new UpdateSolarNodeRequest("Nodo Existente");

        var result = _service.Update(_nodeId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Ya existe un nodo con el nombre \"Nodo Existente\" en este parque solar.", result.ErrorMessage);

        var unchanged = _nodeRepository.GetById(_nodeId);
        Assert.NotNull(unchanged);
        Assert.Equal("Nodo Original", unchanged.Name);
    }

    [Fact]
    public void Update_WhenNameIsSameNodeWithDifferentCasing_ReturnsSuccess()
    {
        var request = new UpdateSolarNodeRequest("NODO ORIGINAL");

        var result = _service.Update(_nodeId, request);

        Assert.True(result.Success);
        var updated = _nodeRepository.GetById(_nodeId);
        Assert.NotNull(updated);
        Assert.Equal("NODO ORIGINAL", updated.Name);
        Assert.Equal("nodo original", updated.NormalizedName);
    }

    [Fact]
    public void Update_WhenSameNameExistsInAnotherPark_IsAllowed()
    {
        _nodeRepository.PersistedNodes.Add(new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo Compartido",
            NormalizedName = "nodo compartido",
            SolarParkId = _anotherParkId,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        var request = new UpdateSolarNodeRequest("Nodo Compartido");

        var result = _service.Update(_nodeId, request);

        Assert.True(result.Success);
        var updated = _nodeRepository.GetById(_nodeId);
        Assert.NotNull(updated);
        Assert.Equal("Nodo Compartido", updated.Name);
        Assert.Equal(_parkId, updated.SolarParkId);
    }

    [Fact]
    public void GetById_WhenNodeExists_ReturnsDtoWithParkName()
    {
        var result = _service.GetById(_nodeId);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(_nodeId, result.Value.Id);
        Assert.Equal("Nodo Original", result.Value.Name);
        Assert.Equal(_parkId, result.Value.SolarParkId);
        Assert.Equal("Parque Salto Norte", result.Value.SolarParkName);
        Assert.Equal("Activo", result.Value.Status);
    }

    [Fact]
    public void GetById_WhenNodeDoesNotExist_ReturnsNotFound()
    {
        var result = _service.GetById(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
    }
}
