using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarParkServiceUpdateTests
{
    private readonly FakeSolarParkRepository _repository = new();
    private readonly SolarParkService _service;

    public SolarParkServiceUpdateTests()
    {
        _service = new SolarParkService(_repository, NullLogger<SolarParkService>.Instance);
    }

    [Fact]
    public void Update_WithValidData_UpdatesParkAndSetsUpdatedAtUtc()
    {
        var id = Guid.NewGuid();
        var originalCreatedDate = DateTime.UtcNow.AddDays(-5);
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Original",
            NormalizedName = "parque original",
            Latitude = -31.0,
            Longitude = -57.0,
            MaxCapacityKw = 1000.0,
            PowerAlertThresholdKw = 500.0,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = originalCreatedDate,
            UpdatedAtUtc = null
        });

        var request = new UpdateSolarParkRequest("Parque Modificado", -32.5, -56.5, 2500.0, 2000.0);

        var result = _service.Update(id, request);

        Assert.True(result.Success);
        var updated = _repository.GetById(id);
        Assert.NotNull(updated);
        Assert.Equal("Parque Modificado", updated.Name);
        Assert.Equal("parque modificado", updated.NormalizedName);
        Assert.Equal(-32.5, updated.Latitude);
        Assert.Equal(-56.5, updated.Longitude);
        Assert.Equal(2500.0, updated.MaxCapacityKw);
        Assert.Equal(2000.0, updated.PowerAlertThresholdKw);
        Assert.Equal(SolarParkStatus.Active, updated.Status);
        Assert.Equal(originalCreatedDate, updated.CreatedAtUtc);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WhenParkDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateSolarParkRequest("Parque Fantasma", -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Update(nonExistentId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El parque solicitado no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WithDuplicateNameOfAnotherPark_ReturnsConflict()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        _repository.Parks.Add(new SolarPark
        {
            Id = id1,
            Name = "Parque Uno",
            NormalizedName = "parque uno",
            Status = SolarParkStatus.Active
        });

        _repository.Parks.Add(new SolarPark
        {
            Id = id2,
            Name = "Parque Dos",
            NormalizedName = "parque dos",
            Status = SolarParkStatus.Active
        });

        // Intentar cambiar Parque Dos al nombre de Parque Uno
        var request = new UpdateSolarParkRequest("  parque uno  ", -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Update(id2, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Ya existe un parque con el nombre \"parque uno\".", result.ErrorMessage);
    }

    [Fact]
    public void Update_ChangingOnlyCaseOfOwnName_ReturnsSuccess()
    {
        var id = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "parque norte",
            NormalizedName = "parque norte",
            Latitude = -31.0,
            Longitude = -57.0,
            MaxCapacityKw = 1000.0,
            PowerAlertThresholdKw = 500.0,
            Status = SolarParkStatus.Active
        });

        var request = new UpdateSolarParkRequest("Parque Norte", -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Update(id, request);

        Assert.True(result.Success);
        var updated = _repository.GetById(id);
        Assert.NotNull(updated);
        Assert.Equal("Parque Norte", updated.Name);
        Assert.Equal("parque norte", updated.NormalizedName);
    }

    [Fact]
    public void Update_WithoutChanges_ReturnsSuccessAndUpdatesDate()
    {
        var id = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Estable",
            NormalizedName = "parque estable",
            Latitude = -31.0,
            Longitude = -57.0,
            MaxCapacityKw = 1000.0,
            PowerAlertThresholdKw = 500.0,
            Status = SolarParkStatus.Active,
            UpdatedAtUtc = null
        });

        var request = new UpdateSolarParkRequest("Parque Estable", -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Update(id, request);

        Assert.True(result.Success);
        var updated = _repository.GetById(id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.UpdatedAtUtc);
    }

    [Fact]
    public void Update_WhenParkIsInactive_AllowsUpdateAndPreservesInactiveStatus()
    {
        var id = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Inactivo",
            NormalizedName = "parque inactivo",
            Latitude = -31.0,
            Longitude = -57.0,
            MaxCapacityKw = 1000.0,
            PowerAlertThresholdKw = 500.0,
            Status = SolarParkStatus.Inactive
        });

        var request = new UpdateSolarParkRequest("Parque Inactivo Renombrado", -32.0, -56.0, 2000.0, 1000.0);

        var result = _service.Update(id, request);

        Assert.True(result.Success);
        var updated = _repository.GetById(id);
        Assert.NotNull(updated);
        Assert.Equal(SolarParkStatus.Inactive, updated.Status);
    }

    [Theory]
    [InlineData("", -31.0, -57.0, 1000.0, 500.0, "El nombre es obligatorio.")]
    [InlineData("Parque", -95.0, -57.0, 1000.0, 500.0, "La latitud debe estar entre -90 y 90.")]
    [InlineData("Parque", -31.0, 200.0, 1000.0, 500.0, "La longitud debe estar entre -180 y 180.")]
    [InlineData("Parque", -31.0, -57.0, 0.0, 0.0, "La capacidad máxima debe ser mayor a 0.")]
    [InlineData("Parque", -31.0, -57.0, 1000.0, 1500.0, "El umbral de alerta debe ser mayor a 0 y no puede superar la capacidad máxima.")]
    public void Update_WithInvalidFields_ReturnsValidationErrors(
        string name, double lat, double lon, double maxCap, double threshold, string expectedError)
    {
        var id = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Valido",
            NormalizedName = "parque valido",
            Latitude = -31.0,
            Longitude = -57.0,
            MaxCapacityKw = 1000.0,
            PowerAlertThresholdKw = 500.0,
            Status = SolarParkStatus.Active
        });

        var request = new UpdateSolarParkRequest(name, lat, lon, maxCap, threshold);

        var result = _service.Update(id, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(expectedError, result.ErrorMessage);
    }

    [Fact]
    public void GetById_WhenParkExists_ReturnsParkDto()
    {
        var id = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = id,
            Name = "Parque Existente",
            NormalizedName = "parque existente",
            Latitude = -31.5,
            Longitude = -57.2,
            MaxCapacityKw = 1500.0,
            PowerAlertThresholdKw = 1200.0,
            Status = SolarParkStatus.Active
        });

        var result = _service.GetById(id);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Parque Existente", result.Value.Name);
        Assert.Equal("Activo", result.Value.Status);
    }

    [Fact]
    public void GetById_WhenParkDoesNotExist_ReturnsNotFound()
    {
        var result = _service.GetById(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El parque solicitado no existe.", result.ErrorMessage);
    }
}
