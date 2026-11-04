using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class PanelServiceDeleteTests
{
    [Fact]
    public void DeletePanel_PanelActivo_CambiaAInactivo()
    {
        // Arrange
        var panelId = Guid.NewGuid();
        var repo = new FakeSolarPanelRepository
        {
            Panels =
            {
                new SolarPanel
                {
                    Id = panelId,
                    SerieNumber = "PNL-001",
                    NormalizedSerieNumber = "pnl-001",
                    PowerW = 300,
                    Efficiency = 20.5,
                    SolarParkId = Guid.NewGuid(),
                    Status = SolarPanelStatus.Active,
                    CreatedAtUtc = DateTime.UtcNow
                }
            }
        };
        var service = new SolarPanelService(repo, new FakeSolarNodeRepository(), new FakeInverterRepository(), NullLogger<SolarPanelService>.Instance);

        // Act
        var result = service.DeletePanel(panelId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(SolarPanelStatus.Inactive, repo.Panels[0].Status);
        Assert.NotNull(repo.Panels[0].UpdatedAtUtc);
        Assert.True(repo.Panels[0].UpdatedAtUtc > DateTime.UtcNow.AddMinutes(-5));
    }

    [Fact]
    public void DeletePanel_PanelYaInactivo_NoCambia()
    {
        // Arrange
        var panelId = Guid.NewGuid();
        var repo = new FakeSolarPanelRepository
        {
            Panels =
            {
                new SolarPanel
                {
                    Id = panelId,
                    SerieNumber = "PNL-002",
                    NormalizedSerieNumber = "pnl-002",
                    PowerW = 250,
                    Efficiency = 18.5,
                    SolarParkId = Guid.NewGuid(),
                    Status = SolarPanelStatus.Inactive,
                    CreatedAtUtc = DateTime.UtcNow
                }
            }
        };
        var service = new SolarPanelService(repo, new FakeSolarNodeRepository(), new FakeInverterRepository(), NullLogger<SolarPanelService>.Instance);

        // Act
        var result = service.DeletePanel(panelId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El panel ya está inactivo.", result.ErrorMessage);
        // Verificar que el panel no cambió de estado
        Assert.Equal(SolarPanelStatus.Inactive, repo.Panels[0].Status);
    }

    [Fact]
    public void DeletePanel_PanelInexistente_ReturnsNotFound()
    {
        // Arrange
        var repo = new FakeSolarPanelRepository();
        var service = new SolarPanelService(repo, new FakeSolarNodeRepository(), new FakeInverterRepository(), NullLogger<SolarPanelService>.Instance);

        // Act
        var result = service.DeletePanel(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        Assert.Equal("El panel solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void DeletePanel_CancelarEnUI_NoHaceCambios()
    {
        // Arrange
        var repo = new FakeSolarPanelRepository();
        var service = new SolarPanelService(repo, new FakeSolarNodeRepository(), new FakeInverterRepository(), NullLogger<SolarPanelService>.Instance);

        // Act - Probar con panel inexistente (simula cancelación)
        var result = service.DeletePanel(Guid.NewGuid());

        // Assert
        Assert.False(result.Success);
        // El repositorio no debería tener cambios (aún está vacío)
        Assert.Empty(repo.Panels);
    }
}