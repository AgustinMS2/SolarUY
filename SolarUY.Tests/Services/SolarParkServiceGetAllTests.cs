using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarParkServiceGetAllTests
{
    private readonly FakeSolarParkRepository _repository = new();
    private readonly SolarParkService _service;

    public SolarParkServiceGetAllTests()
    {
        _service = new SolarParkService(_repository, NullLogger<SolarParkService>.Instance);
    }

    [Fact]
    public void GetAll_WhenNoParks_ReturnsEmptyList()
    {
        var result = _service.GetAll();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAll_OrdersAlphabeticallyIgnoringCase()
    {
        // Caso límite de spec: "alfa", "Beta" y "gamma" se ordenan alfa, Beta, gamma
        _repository.Parks.Add(new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "gamma",
            NormalizedName = "gamma",
            Latitude = -31.0,
            Longitude = -57.0,
            MaxCapacityKw = 1000.0,
            PowerAlertThresholdKw = 500.0,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        _repository.Parks.Add(new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "alfa",
            NormalizedName = "alfa",
            Latitude = -32.0,
            Longitude = -56.0,
            MaxCapacityKw = 2000.0,
            PowerAlertThresholdKw = 1500.0,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        _repository.Parks.Add(new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Beta",
            NormalizedName = "beta",
            Latitude = -33.0,
            Longitude = -55.0,
            MaxCapacityKw = 3000.0,
            PowerAlertThresholdKw = 2500.0,
            Status = SolarParkStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow
        });

        var result = _service.GetAll();

        Assert.Equal(3, result.Count);
        Assert.Equal("alfa", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal("gamma", result[2].Name);
    }

    [Fact]
    public void GetAll_TranslatesStatusCorrectly()
    {
        _repository.Parks.Add(new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Activo",
            NormalizedName = "parque activo",
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        });

        _repository.Parks.Add(new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Inactivo",
            NormalizedName = "parque inactivo",
            Status = SolarParkStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow
        });

        var result = _service.GetAll();

        Assert.Equal(2, result.Count);
        var activePark = result.First(p => p.Name == "Parque Activo");
        var inactivePark = result.First(p => p.Name == "Parque Inactivo");

        Assert.Equal("Activo", activePark.Status);
        Assert.Equal("Inactivo", inactivePark.Status);
    }
}
