using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarNodeServiceCreateTests
{
    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndPersistsNodeActive()
    {
        // Arrange
        var parkId = Guid.NewGuid();
        var parkRepo = new FakeSolarParkRepository
        {
            Parks =
            {
                new SolarPark { Id = parkId, Name = "Parque Solar Test", NormalizedName = "parque solar test", Status = SolarParkStatus.Active }
            }
        };
        var nodeRepo = new FakeSolarNodeRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);
        var request = new CreateSolarNodeRequest("Nodo Test", parkId);

        // Act
        var result = service.Create(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Single(nodeRepo.PersistedNodes);

        var savedNode = nodeRepo.PersistedNodes[0];
        Assert.Equal(result.Value, savedNode.Id);
        Assert.Equal("Nodo Test", savedNode.Name);
        Assert.Equal("nodo test", savedNode.NormalizedName);
        Assert.Equal(SolarNodeStatus.Active, savedNode.Status);
        Assert.True(savedNode.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithLeadingAndTrailingSpacesInName_SavesTrimmedName()
    {
        // Arrange
        var parkId = Guid.NewGuid();
        var parkRepo = new FakeSolarParkRepository
        {
            Parks =
            {
                new SolarPark { Id = parkId, Name = "Parque Solar Test", NormalizedName = "parque solar test", Status = SolarParkStatus.Active }
            }
        };
        var nodeRepo = new FakeSolarNodeRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);
        var request = new CreateSolarNodeRequest("  Nodo Norte  ", parkId);

        // Act
        var result = service.Create(request);

        // Assert
        Assert.True(result.Success);
        Assert.Single(nodeRepo.PersistedNodes);
        Assert.Equal("Nodo Norte", nodeRepo.PersistedNodes[0].Name);
        Assert.Equal("nodo norte", nodeRepo.PersistedNodes[0].NormalizedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceName_ReturnsValidationError(string? name)
    {
        // Arrange
        var parkId = Guid.NewGuid();
        var parkRepo = new FakeSolarParkRepository
        {
            Parks =
            {
                new SolarPark { Id = parkId, Name = "Parque Solar Test", NormalizedName = "parque solar test", Status = SolarParkStatus.Active }
            }
        };
        var nodeRepo = new FakeSolarNodeRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);
        var request = new CreateSolarNodeRequest(name!, parkId);

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre es obligatorio.", result.ErrorMessage);
        Assert.Empty(nodeRepo.PersistedNodes);
    }

    [Fact]
    public void Create_WithNameLongerThan100Characters_ReturnsValidationError()
    {
        // Arrange
        var parkId = Guid.NewGuid();
        var parkRepo = new FakeSolarParkRepository
        {
            Parks =
            {
                new SolarPark { Id = parkId, Name = "Parque Solar Test", NormalizedName = "parque solar test", Status = SolarParkStatus.Active }
            }
        };
        var nodeRepo = new FakeSolarNodeRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);
        var longName = new string('A', 101);
        var request = new CreateSolarNodeRequest(longName, parkId);

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre no puede superar los 100 caracteres.", result.ErrorMessage);
        Assert.Empty(nodeRepo.PersistedNodes);
    }

    [Theory]
    [InlineData("NONEXISTENT-PARK")]
    public void Create_WithNonExistentPark_ReturnsError(string nonExistentParkName)
    {
        // Arrange: Sin parques en el repositorio (park inexistente)
        var parkRepo = new FakeSolarParkRepository(); // Sin parques añadidos
        var nodeRepo = new FakeSolarNodeRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);
        var request = new CreateSolarNodeRequest("Nodo Fantasma", Guid.NewGuid());

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        // Parque inexistente debe devolver error (NotFound o Validation)
        Assert.True(result.ErrorType == ServiceErrorType.NotFound || result.ErrorType == ServiceErrorType.Validation);
        Assert.Empty(nodeRepo.PersistedNodes);
    }

    [Fact]
    public void Create_WithDuplicateNameInSamePark_ReturnsConflictError()
    {
        // Arrange
        var parkId = Guid.NewGuid();
        var parkRepo = new FakeSolarParkRepository
        {
            Parks =
            {
                new SolarPark { Id = parkId, Name = "Parque Solar Test", NormalizedName = "parque solar test", Status = SolarParkStatus.Active }
            }
        };
        var nodeRepo = new FakeSolarNodeRepository();
        // Insertar un nodo existente (simulando que ya fue creado)
        nodeRepo.PersistedNodes.Add(new SolarNode
        {
            Id = Guid.NewGuid(),
            Name = "Nodo Duplicado",
            NormalizedName = "nodo duplicado",
            SolarParkId = parkId,
            Status = SolarNodeStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);
        var request = new CreateSolarNodeRequest("Nodo Duplicado", parkId);

        // Act
        var result = service.Create(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Ya existe un nodo con el nombre \"Nodo Duplicado\" en este parque solar.", result.ErrorMessage);
        Assert.Single(nodeRepo.PersistedNodes); // No se agregó otro
    }

    [Fact]
    public void Create_WithSameNameDifferentPark_IsValid()
    {
        // Arrange
        var park1Id = Guid.NewGuid();
        var park2Id = Guid.NewGuid();
        var parkRepo = new FakeSolarParkRepository
        {
            Parks =
            {
                new SolarPark { Id = park1Id, Name = "Parque Uno", NormalizedName = "parque uno", Status = SolarParkStatus.Active },
                new SolarPark { Id = park2Id, Name = "Parque Dos", NormalizedName = "parque dos", Status = SolarParkStatus.Active }
            }
        };
        var nodeRepo = new FakeSolarNodeRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);

        // Act & Assert 1: Crear un nodo en el primer parque
        var request1 = new CreateSolarNodeRequest("Nodo Común", park1Id);
        var result1 = service.Create(request1);
        Assert.True(result1.Success);
        Assert.Single(nodeRepo.PersistedNodes);

        // Act & Assert 2: Crear otro nodo con el mismo nombre en parque diferente
        var request2 = new CreateSolarNodeRequest("Nodo Común", park2Id);
        var result2 = service.Create(request2);
        Assert.True(result2.Success);
        Assert.Equal(2, nodeRepo.PersistedNodes.Count);
    }
}