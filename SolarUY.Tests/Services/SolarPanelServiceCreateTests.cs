using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class SolarPanelServiceCreateTests
{
    private readonly FakeSolarPanelRepository _repository = new();
    private readonly SolarPanelService _service;
    private readonly Guid _activeParkId = Guid.NewGuid();

    public SolarPanelServiceCreateTests()
    {
        _service = new SolarPanelService(_repository, NullLogger<SolarPanelService>.Instance);

        _repository.Parks.Add(new SolarPark
        {
            Id = _activeParkId,
            Name = "Parque Salto Norte",
            NormalizedName = "parque salto norte",
            Status = SolarParkStatus.Active
        });
    }

    [Fact]
    public void Create_WithValidData_ReturnsSuccessAndPersistsPanelWithTrimmedSerieAndActiveStatus()
    {
        var request = new CreateSolarPanelRequest(
            SerieNumber: "  PNL-2026-001  ",
            PowerW: 450,
            Efficiency: 21.5,
            SolarParkId: _activeParkId
        );

        var result = _service.Create(request);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Value);

        var panel = _repository.Panels.FirstOrDefault(p => p.Id == result.Value);
        Assert.NotNull(panel);
        Assert.Equal("PNL-2026-001", panel.SerieNumber);
        Assert.Equal("pnl-2026-001", panel.NormalizedSerieNumber);
        Assert.Equal(450, panel.PowerW);
        Assert.Equal(21.5, panel.Efficiency);
        Assert.Equal(_activeParkId, panel.SolarParkId);
        Assert.Equal(SolarPanelStatus.Active, panel.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenSerieNumberIsNullOrWhitespace_ReturnsValidationError(string? invalidSerie)
    {
        var request = new CreateSolarPanelRequest(invalidSerie, 400, 20.0, _activeParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El número de serie es obligatorio.", result.ErrorMessage);
    }

    [Fact]
    public void Create_WhenSerieNumberExceeds50Characters_ReturnsValidationError()
    {
        var longSerie = new string('A', 51);
        var request = new CreateSolarPanelRequest(longSerie, 400, 20.0, _activeParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El número de serie no puede superar los 50 caracteres.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WhenPowerWIsZeroOrNegative_ReturnsValidationError(int invalidPower)
    {
        var request = new CreateSolarPanelRequest("PNL-001", invalidPower, 20.0, _activeParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La potencia debe ser mayor a 0.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(-5.0)]
    public void Create_WhenEfficiencyIsZeroOrNegative_ReturnsValidationError(double invalidEfficiency)
    {
        var request = new CreateSolarPanelRequest("PNL-001", 400, invalidEfficiency, _activeParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La eficiencia debe ser mayor a 0.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(100.1)]
    [InlineData(150.0)]
    public void Create_WhenEfficiencyExceeds100_ReturnsValidationError(double invalidEfficiency)
    {
        var request = new CreateSolarPanelRequest("PNL-001", 400, invalidEfficiency, _activeParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La eficiencia no puede superar el 100.", result.ErrorMessage);
    }

    [Fact]
    public void Create_WhenSolarParkDoesNotExist_ReturnsValidationError()
    {
        var nonExistentParkId = Guid.NewGuid();
        var request = new CreateSolarPanelRequest("PNL-001", 400, 20.0, nonExistentParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El parque solar seleccionado no existe o no está activo.", result.ErrorMessage);
    }

    [Fact]
    public void Create_WhenSolarParkIsInactive_ReturnsValidationError()
    {
        var inactiveParkId = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = inactiveParkId,
            Name = "Parque Inactivo",
            NormalizedName = "parque inactivo",
            Status = SolarParkStatus.Inactive
        });

        var request = new CreateSolarPanelRequest("PNL-001", 400, 20.0, inactiveParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El parque solar seleccionado no existe o no está activo.", result.ErrorMessage);
    }

    [Fact]
    public void Create_WhenSerieNumberAlreadyExistsCaseInsensitive_ReturnsConflict()
    {
        _repository.Panels.Add(new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PNL-ABC-123",
            NormalizedSerieNumber = "pnl-abc-123",
            PowerW = 400,
            Efficiency = 20.0,
            SolarParkId = _activeParkId,
            Status = SolarPanelStatus.Active
        });

        var request = new CreateSolarPanelRequest("  pnl-abc-123  ", 500, 22.0, _activeParkId);

        var result = _service.Create(request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Ya existe un panel con el número de serie \"pnl-abc-123\".", result.ErrorMessage);
    }
}
