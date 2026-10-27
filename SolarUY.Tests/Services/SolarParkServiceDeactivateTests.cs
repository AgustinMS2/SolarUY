using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarParkServiceDeactivateTests
{
    private readonly FakeSolarParkRepository _repository = new();
    private readonly SolarParkService _service;

    public SolarParkServiceDeactivateTests()
    {
        _service = new SolarParkService(_repository, NullLogger<SolarParkService>.Instance);
    }

    [Fact]
    public void Deactivate_WhenParkIsActive_DeactivatesParkAndSetsUpdatedAtUtc()
    {
        var id = Guid.NewGuid();
        var originalCreatedDate = DateTime.UtcNow.AddDays(-2);
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Salto",
            NormalizedName = "parque salto",
            Latitude = -31.38,
            Longitude = -57.96,
            MaxCapacityKw = 5000,
            PowerAlertThresholdKw = 4000,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = originalCreatedDate,
            UpdatedAtUtc = null
        });

        var result = _service.Deactivate(id);

        Assert.True(result.Success);
        var deactivated = _repository.GetById(id);
        Assert.NotNull(deactivated);
        Assert.Equal(SolarParkStatus.Inactive, deactivated.Status);
        Assert.Equal(originalCreatedDate, deactivated.CreatedAtUtc);
        Assert.NotNull(deactivated.UpdatedAtUtc);
    }

    [Fact]
    public void Deactivate_WhenParkDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var result = _service.Deactivate(nonExistentId);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El parque solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Deactivate_WhenParkAlreadyInactive_ReturnsConflict()
    {
        var id = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Ya Inactivo",
            NormalizedName = "parque ya inactivo",
            Latitude = -32.0,
            Longitude = -56.0,
            MaxCapacityKw = 2000,
            PowerAlertThresholdKw = 1500,
            Status = SolarParkStatus.Inactive,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });

        var result = _service.Deactivate(id);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("El parque ya está inactivo.", result.ErrorMessage);
    }
}
