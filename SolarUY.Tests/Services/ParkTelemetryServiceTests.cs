using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class ParkTelemetryServiceTests
{
    private readonly ParkTelemetryService _service = new();

    private static SolarPark CreateActivePark(double maxCapacityKw = 500, double thresholdKw = 200)
    {
        return new SolarPark
        {
            Id = Guid.NewGuid(),
            Name = "Parque Salto Solar",
            NormalizedName = "parque salto solar",
            MaxCapacityKw = maxCapacityKw,
            PowerAlertThresholdKw = thresholdKw,
            Status = SolarParkStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public void CalculateMetrics_WithNullPark_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _service.CalculateMetrics(null!, 100, 500, new DateTime(2026, 11, 12, 12, 0, 0)));
    }

    [Fact]
    public void CalculateMetrics_DuringDaylight_CalculatesCapacityPercentageAccurately()
    {
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 100);
        var midday = new DateTime(2026, 11, 12, 12, 30, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 375, energyTodayKwh: 1250.5, midday);

        Assert.Equal(park.Id, result.ParkId);
        Assert.Equal("Parque Salto Solar", result.ParkName);
        Assert.Equal(375, result.CurrentPowerKw);
        Assert.Equal(500, result.MaxCapacityKw);
        Assert.Equal(75.0, result.CapacityPercentage);
        Assert.Equal(1250.5, result.EnergyTodayKwh);
        Assert.Equal(100, result.AlertThresholdKw);
        Assert.False(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_DuringDaylight_WhenPowerBelowThreshold_ActivatesAlert()
    {
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 300);
        var morning = new DateTime(2026, 11, 12, 10, 0, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 150, energyTodayKwh: 300, morning);

        Assert.Equal(30.0, result.CapacityPercentage);
        Assert.True(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_DuringNight_SetsPowerToZeroAndSuppressesAlert()
    {
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 250);
        var night = new DateTime(2026, 11, 12, 23, 15, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 0, energyTodayKwh: 2400, night);

        Assert.Equal(0.0, result.CurrentPowerKw);
        Assert.Equal(0.0, result.CapacityPercentage);
        Assert.False(result.IsAlertActive);
        Assert.Equal(2400, result.EnergyTodayKwh);
    }

    [Fact]
    public void CalculateMetrics_WhenParkIsInactive_SetsPowerToZeroAndSuppressesAlert()
    {
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 200);
        park.Status = SolarParkStatus.Inactive;
        var midday = new DateTime(2026, 11, 12, 12, 0, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 400, energyTodayKwh: 500, midday);

        Assert.Equal(0.0, result.CurrentPowerKw);
        Assert.Equal(0.0, result.CapacityPercentage);
        Assert.False(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_WhenMaxCapacityIsZero_ReturnsZeroPercentageWithoutException()
    {
        var park = CreateActivePark(maxCapacityKw: 0, thresholdKw: 0);
        var afternoon = new DateTime(2026, 11, 12, 15, 0, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 50, energyTodayKwh: 100, afternoon);

        Assert.Equal(0.0, result.CapacityPercentage);
    }
}

