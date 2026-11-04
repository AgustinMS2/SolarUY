using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarPanelServiceGetAllTests
{
    private readonly FakeSolarPanelRepository _repository = new();
    private readonly SolarPanelService _service;

    public SolarPanelServiceGetAllTests()
    {
        _service = new SolarPanelService(_repository, new FakeSolarNodeRepository(), new FakeInverterRepository(), NullLogger<SolarPanelService>.Instance);
    }

    [Fact]
    public void GetAll_WhenNoPanelsExist_ReturnsEmptyList()
    {
        var result = _service.GetAll();

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_ReturnsPanelsOrderedAlphabeticallyBySerieNumberCaseInsensitive()
    {
        var park = new SolarPark { Id = Guid.NewGuid(), Name = "Parque Rocha", Status = SolarParkStatus.Active };
        _repository.Parks.Add(park);

        _repository.Panels.Add(new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "gamma-001",
            NormalizedSerieNumber = "gamma-001",
            PowerW = 400,
            Efficiency = 20.0,
            SolarParkId = park.Id,
            SolarPark = park,
            Status = SolarPanelStatus.Active
        });

        _repository.Panels.Add(new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "alfa-001",
            NormalizedSerieNumber = "alfa-001",
            PowerW = 450,
            Efficiency = 21.0,
            SolarParkId = park.Id,
            SolarPark = park,
            Status = SolarPanelStatus.Active
        });

        _repository.Panels.Add(new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "Beta-001",
            NormalizedSerieNumber = "beta-001",
            PowerW = 500,
            Efficiency = 22.0,
            SolarParkId = park.Id,
            SolarPark = park,
            Status = SolarPanelStatus.Inactive
        });

        var result = _service.GetAll();

        Assert.Equal(3, result.Count);
        Assert.Equal("alfa-001", result[0].SerieNumber);
        Assert.Equal("Beta-001", result[1].SerieNumber);
        Assert.Equal("gamma-001", result[2].SerieNumber);
    }

    [Fact]
    public void GetAll_MapsStatusAndSolarParkNameCorrectly()
    {
        var park = new SolarPark { Id = Guid.NewGuid(), Name = "Parque Salto Grande", Status = SolarParkStatus.Active };
        _repository.Parks.Add(park);

        _repository.Panels.Add(new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PNL-01",
            NormalizedSerieNumber = "pnl-01",
            PowerW = 400,
            Efficiency = 20.5,
            SolarParkId = park.Id,
            SolarPark = park,
            Status = SolarPanelStatus.Active
        });

        _repository.Panels.Add(new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PNL-02",
            NormalizedSerieNumber = "pnl-02",
            PowerW = 450,
            Efficiency = 21.5,
            SolarParkId = park.Id,
            SolarPark = park,
            Status = SolarPanelStatus.Inactive
        });

        var result = _service.GetAll();

        Assert.Equal(2, result.Count);
        var activePanel = result.First(p => p.SerieNumber == "PNL-01");
        var inactivePanel = result.First(p => p.SerieNumber == "PNL-02");

        Assert.Equal("Activo", activePanel.Status);
        Assert.Equal("Parque Salto Grande", activePanel.SolarParkName);
        Assert.Equal(400, activePanel.PowerW);
        Assert.Equal(20.5, activePanel.Efficiency);

        Assert.Equal("Inactivo", inactivePanel.Status);
        Assert.Equal("Parque Salto Grande", inactivePanel.SolarParkName);
    }
}
