using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class PanelServiceUpdateTests
{
    private readonly FakeSolarPanelRepository _repository = new();
    private readonly SolarPanelService _service;
    private readonly Guid _activeParkId = Guid.NewGuid();
    private readonly Guid _anotherParkId = Guid.NewGuid();
    private readonly Guid _panelId = Guid.NewGuid();
    private readonly DateTime _initialCreatedUtc = DateTime.UtcNow.AddDays(-10);

    public PanelServiceUpdateTests()
    {
        _service = new SolarPanelService(_repository, NullLogger<SolarPanelService>.Instance);

        _repository.Parks.Add(new SolarPark
        {
            Id = _activeParkId,
            Name = "Parque Salto Norte",
            NormalizedName = "parque salto norte",
            Status = SolarParkStatus.Active
        });

        _repository.Parks.Add(new SolarPark
        {
            Id = _anotherParkId,
            Name = "Parque Paysandú",
            NormalizedName = "parque paysandú",
            Status = SolarParkStatus.Active
        });

        _repository.Panels.Add(new SolarPanel
        {
            Id = _panelId,
            SerieNumber = "PNL-ORIGINAL-01",
            NormalizedSerieNumber = "pnl-original-01",
            PowerW = 400,
            Efficiency = 20.0,
            SolarParkId = _activeParkId,
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = _initialCreatedUtc,
            UpdatedAtUtc = null
        });
    }

    [Fact]
    public void Update_WithValidData_ReturnsSuccessAndUpdatesPanelFieldsAndDate()
    {
        var request = new UpdateSolarPanelRequest(
            SerieNumber: "  PNL-UPDATED-01  ",
            PowerW: 550,
            Efficiency: 22.5,
            SolarParkId: _anotherParkId
        );

        var result = _service.Update(_panelId, request);

        Assert.True(result.Success);
        var updated = _repository.GetById(_panelId);
        Assert.NotNull(updated);
        Assert.Equal("PNL-UPDATED-01", updated.SerieNumber);
        Assert.Equal("pnl-updated-01", updated.NormalizedSerieNumber);
        Assert.Equal(550, updated.PowerW);
        Assert.Equal(22.5, updated.Efficiency);
        Assert.Equal(_anotherParkId, updated.SolarParkId);
        Assert.Equal(_initialCreatedUtc, updated.CreatedAtUtc);
        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.Equal(SolarPanelStatus.Active, updated.Status);
    }

    [Fact]
    public void Update_WhenPanelDoesNotExist_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();
        var request = new UpdateSolarPanelRequest("PNL-999", 500, 21.0, _activeParkId);

        var result = _service.Update(nonExistentId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El panel solicitado no existe.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WhenSerieNumberIsNullOrWhitespace_ReturnsValidationError(string? invalidSerie)
    {
        var request = new UpdateSolarPanelRequest(invalidSerie, 500, 21.0, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El número de serie es obligatorio.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenSerieNumberExceeds50Characters_ReturnsValidationError()
    {
        var longSerie = new string('X', 51);
        var request = new UpdateSolarPanelRequest(longSerie, 500, 21.0, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El número de serie no puede superar los 50 caracteres.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_WhenPowerWIsZeroOrNegative_ReturnsValidationError(int invalidPower)
    {
        var request = new UpdateSolarPanelRequest("PNL-01", invalidPower, 21.0, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La potencia debe ser mayor a 0.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    public void Update_WhenEfficiencyIsZeroOrNegative_ReturnsValidationError(double invalidEfficiency)
    {
        var request = new UpdateSolarPanelRequest("PNL-01", 500, invalidEfficiency, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La eficiencia debe ser mayor a 0.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(100.1)]
    [InlineData(120.0)]
    public void Update_WhenEfficiencyExceeds100_ReturnsValidationError(double invalidEfficiency)
    {
        var request = new UpdateSolarPanelRequest("PNL-01", 500, invalidEfficiency, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("La eficiencia no puede superar el 100.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenSolarParkDoesNotExist_ReturnsValidationError()
    {
        var nonExistentParkId = Guid.NewGuid();
        var request = new UpdateSolarPanelRequest("PNL-01", 500, 21.0, nonExistentParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El parque solar seleccionado no existe o no está activo.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenSolarParkIsInactive_ReturnsValidationError()
    {
        var inactiveParkId = Guid.NewGuid();
        _repository.Parks.Add(new SolarPark
        {
            Id = inactiveParkId,
            Name = "Parque Inactivo",
            NormalizedName = "parque inactivo",
            Status = SolarParkStatus.Inactive
        });

        var request = new UpdateSolarPanelRequest("PNL-01", 500, 21.0, inactiveParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal("El parque solar seleccionado no existe o no está activo.", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenSerieNumberIsUsedByAnotherPanel_ReturnsConflict()
    {
        var anotherPanelId = Guid.NewGuid();
        _repository.Panels.Add(new SolarPanel
        {
            Id = anotherPanelId,
            SerieNumber = "PNL-EXISTS-99",
            NormalizedSerieNumber = "pnl-exists-99",
            PowerW = 400,
            Efficiency = 20.0,
            SolarParkId = _activeParkId,
            Status = SolarPanelStatus.Active
        });

        var request = new UpdateSolarPanelRequest("pnl-exists-99", 500, 21.0, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Ya existe un panel con el número de serie \"pnl-exists-99\".", result.ErrorMessage);
    }

    [Fact]
    public void Update_WhenSerieNumberIsSamePanelWithDifferentCasing_ReturnsSuccess()
    {
        var request = new UpdateSolarPanelRequest("pnl-original-01", 450, 21.0, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.True(result.Success);
        var panel = _repository.GetById(_panelId);
        Assert.NotNull(panel);
        Assert.Equal("pnl-original-01", panel.SerieNumber);
        Assert.Equal(450, panel.PowerW);
    }

    [Fact]
    public void Update_WhenPanelIsInactive_AllowsUpdateAndPreservesInactiveStatus()
    {
        var inactivePanel = _repository.GetById(_panelId);
        Assert.NotNull(inactivePanel);
        inactivePanel.Status = SolarPanelStatus.Inactive;

        var request = new UpdateSolarPanelRequest("PNL-ORIGINAL-01", 480, 22.0, _activeParkId);

        var result = _service.Update(_panelId, request);

        Assert.True(result.Success);
        var updated = _repository.GetById(_panelId);
        Assert.NotNull(updated);
        Assert.Equal(SolarPanelStatus.Inactive, updated.Status);
        Assert.Equal(480, updated.PowerW);
    }

    [Fact]
    public void GetById_WhenPanelExists_ReturnsSuccessWithDto()
    {
        var result = _service.GetById(_panelId);

        Assert.True(result.Success);
        Assert.NotNull(result.Value);
        Assert.Equal(_panelId, result.Value.Id);
        Assert.Equal("PNL-ORIGINAL-01", result.Value.SerieNumber);
        Assert.Equal("Activo", result.Value.Status);
    }

    [Fact]
    public void GetById_WhenPanelDoesNotExist_ReturnsNotFound()
    {
        var result = _service.GetById(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("El panel solicitado no existe.", result.ErrorMessage);
    }
}
