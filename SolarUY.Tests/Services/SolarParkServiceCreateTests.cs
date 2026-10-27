using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarParkServiceCreateTests
{
    private readonly FakeSolarParkRepository _repository = new();
    private readonly SolarParkService _service;

    public SolarParkServiceCreateTests()
    {
        _service = new SolarParkService(_repository, NullLogger<SolarParkService>.Instance);
    }

    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndPersistsParkActive()
    {
        var request = new CreateSolarParkRequest("Parque Salto", -31.38, -57.96, 5000.0, 4500.0);

        var result = _service.Create(request);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Value);
        Assert.Single(_repository.Parks);

        var savedPark = _repository.Parks[0];
        Assert.Equal(result.Value, savedPark.Id);
        Assert.Equal("Parque Salto", savedPark.Name);
        Assert.Equal("parque salto", savedPark.NormalizedName);
        Assert.Equal(-31.38, savedPark.Latitude);
        Assert.Equal(-57.96, savedPark.Longitude);
        Assert.Equal(5000.0, savedPark.MaxCapacityKw);
        Assert.Equal(4500.0, savedPark.PowerAlertThresholdKw);
        Assert.Equal(SolarParkStatus.Active, savedPark.Status);
        Assert.True(savedPark.CreatedAtUtc <= DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithLeadingAndTrailingSpacesInName_SavesTrimmedName()
    {
        var request = new CreateSolarParkRequest("  Parque Norte  ", -30.0, -55.0, 1000.0, 900.0);

        var result = _service.Create(request);

        Assert.True(result.Success);
        Assert.Single(_repository.Parks);
        Assert.Equal("Parque Norte", _repository.Parks[0].Name);
        Assert.Equal("parque norte", _repository.Parks[0].NormalizedName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespaceName_ReturnsValidationError(string? name)
    {
        var request = new CreateSolarParkRequest(name!, -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre es obligatorio.", result.ErrorMessage);
        Assert.Empty(_repository.Parks);
    }

    [Fact]
    public void Create_WithNameLongerThan100Characters_ReturnsValidationError()
    {
        var longName = new string('A', 101);
        var request = new CreateSolarParkRequest(longName, -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El nombre no puede superar los 100 caracteres.", result.ErrorMessage);
        Assert.Empty(_repository.Parks);
    }

    [Theory]
    [InlineData(-90.001)]
    [InlineData(90.001)]
    public void Create_WithLatitudeOutOfRange_ReturnsValidationError(double latitude)
    {
        var request = new CreateSolarParkRequest("Parque Sur", latitude, -57.0, 1000.0, 500.0);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La latitud debe estar entre -90 y 90.", result.ErrorMessage);
        Assert.Empty(_repository.Parks);
    }

    [Theory]
    [InlineData(-180.001)]
    [InlineData(180.001)]
    public void Create_WithLongitudeOutOfRange_ReturnsValidationError(double longitude)
    {
        var request = new CreateSolarParkRequest("Parque Este", -31.0, longitude, 1000.0, 500.0);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La longitud debe estar entre -180 y 180.", result.ErrorMessage);
        Assert.Empty(_repository.Parks);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-100.0)]
    public void Create_WithCapacityLessThanOrEqualToZero_ReturnsValidationError(double capacity)
    {
        var request = new CreateSolarParkRequest("Parque Oeste", -31.0, -57.0, capacity, 0.0);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La capacidad máxima debe ser mayor a 0.", result.ErrorMessage);
        Assert.Empty(_repository.Parks);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-50.0)]
    [InlineData(1001.0)] // Mayor a la capacidad de 1000
    public void Create_WithInvalidThreshold_ReturnsValidationError(double threshold)
    {
        var request = new CreateSolarParkRequest("Parque Centro", -31.0, -57.0, 1000.0, threshold);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El umbral de alerta debe ser mayor a 0 y no puede superar la capacidad máxima.", result.ErrorMessage);
        Assert.Empty(_repository.Parks);
    }

    [Fact]
    public void Create_WithThresholdEqualToMaxCapacity_ReturnsSuccess()
    {
        var request = new CreateSolarParkRequest("Parque Limite", -31.0, -57.0, 1000.0, 1000.0);

        var result = _service.Create(request);

        Assert.True(result.Success);
        Assert.Single(_repository.Parks);
    }

    [Fact]
    public void Create_WithDuplicateNormalizedName_ReturnsConflictError()
    {
        _repository.Parks.Add(new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Solar Norte",
            NormalizedName = "parque solar norte",
            Status = SolarParkStatus.Active
        });

        // Caso diferente mayúsculas y espacios
        var request = new CreateSolarParkRequest("  PARQUE SOLAR NORTE  ", -31.0, -57.0, 1000.0, 500.0);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Ya existe un parque con el nombre \"PARQUE SOLAR NORTE\".", result.ErrorMessage);
        Assert.Single(_repository.Parks); // No se agregó otro
    }
}
