using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarNodeServiceDeactivateTests
{
    [Fact]
    public void Deactivate_ActiveNode_PassesToInactiveWithDate()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeRepo = new FakeSolarNodeRepository
        {
            PersistedNodes =
            {
                new SolarNode
                {
                    Id = nodeId,
                    Name = "Nodo Test",
                    NormalizedName = "nodo test",
                    SolarParkId = Guid.NewGuid(),
                    Status = SolarNodeStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow
                }
            }
        };
        var parkRepo = new FakeSolarParkRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);

        // Act
        var result = service.Deactivate(nodeId);

        // Assert
        Assert.True(result.Success);
        var savedNode = nodeRepo.PersistedNodes[0];
        Assert.Equal(SolarNodeStatus.Inactive, savedNode.Status);
        Assert.NotNull(savedNode.UpdatedAtUtc);
        Assert.True(savedNode.UpdatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Deactivate_NonExistentNode_ReturnsNotFound()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeRepo = new FakeSolarNodeRepository();
        var parkRepo = new FakeSolarParkRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);

        // Act
        var result = service.Deactivate(nodeId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El nodo solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Deactivate_InactiveNode_ReturnsConflict()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var nodeRepo = new FakeSolarNodeRepository
        {
            PersistedNodes =
            {
                new SolarNode
                {
                    Id = nodeId,
                    Name = "Nodo Inactivo",
                    NormalizedName = "nodo inactivo",
                    SolarParkId = Guid.NewGuid(),
                    Status = SolarNodeStatus.Inactive,
                    CreatedAtUtc = DateTime.UtcNow
                }
            }
        };
        var parkRepo = new FakeSolarParkRepository();
        var service = new SolarNodeService(nodeRepo, parkRepo, new FakeInverterRepository(), new FakeSolarPanelRepository(), NullLogger<SolarNodeService>.Instance);

        // Act
        var result = service.Deactivate(nodeId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("El nodo ya está inactivo.", result.ErrorMessage);
    }
}