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

    // Spec 033: durante el corte administrativo la potencia entregada se reporta en 0 kW
    // y no se generan alertas de generación baja (de día y por debajo del umbral).
    [Fact]
    public void CalculateMetrics_WhenInjectionIsCut_SetsPowerToZeroAndSuppressesAlert()
    {
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 300);
        park.GridInjectionEnabled = false;
        var morning = new DateTime(2026, 11, 12, 10, 0, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 150, energyTodayKwh: 300, morning);

        Assert.Equal(0.0, result.CurrentPowerKw);
        Assert.Equal(0.0, result.CapacityPercentage);
        Assert.False(result.IsAlertActive);
    }

    // ---------- Horas de sol según la época del año (spec 036) ----------

    [Fact]
    public void CalculateMetrics_InWinterBeforeSunrise_IsStillNightAndSuppressesAlert()
    {
        // 21 de junio: el sol sale a las 07:42, por lo que a las 07:00 todavía es de noche.
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 250);

        var result = _service.CalculateMetrics(park, currentPowerKw: 80, energyTodayKwh: 0, new DateTime(2026, 6, 21, 7, 0, 0));

        Assert.Equal(0.0, result.CurrentPowerKw);
        Assert.False(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_InSummerAtTheSameHour_IsAlreadyDaylight()
    {
        // 21 de diciembre: el sol sale a las 05:36, por lo que a las 07:00 ya hay sol y la generación baja alerta.
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 250);

        var result = _service.CalculateMetrics(park, currentPowerKw: 80, energyTodayKwh: 0, new DateTime(2026, 12, 21, 7, 0, 0));

        Assert.Equal(80.0, result.CurrentPowerKw);
        Assert.True(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_InSummerAfterEightPm_IsStillDaylightUntilSunset()
    {
        // 21 de diciembre: el sol se pone a las 19:54.
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 250);

        var beforeSunset = _service.CalculateMetrics(park, 40, 3000, new DateTime(2026, 12, 21, 19, 45, 0));
        var afterSunset = _service.CalculateMetrics(park, 40, 3000, new DateTime(2026, 12, 21, 20, 5, 0));

        Assert.Equal(40.0, beforeSunset.CurrentPowerKw);
        Assert.True(beforeSunset.IsAlertActive);
        Assert.Equal(0.0, afterSunset.CurrentPowerKw);
        Assert.False(afterSunset.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_InWinterAfterFivePm_IsNightFromTheSunsetOnwards()
    {
        // 21 de junio: el sol se pone a las 17:48.
        var park = CreateActivePark(maxCapacityKw: 500, thresholdKw: 250);

        var result = _service.CalculateMetrics(park, currentPowerKw: 30, energyTodayKwh: 1200, new DateTime(2026, 6, 21, 18, 0, 0));

        Assert.Equal(0.0, result.CurrentPowerKw);
        Assert.False(result.IsAlertActive);
    }

    // ---------- Parque sin equipos que generen (spec 036) ----------

    [Fact]
    public void CalculateMetrics_ParkWithoutEquipment_DoesNotRaiseTheLowGenerationAlertDuringDaylight()
    {
        var park = CreateActivePark(maxCapacityKw: 250, thresholdKw: 40);
        var midday = new DateTime(2026, 11, 12, 12, 30, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 0, energyTodayKwh: 0, midday, hasEquipment: false);

        Assert.Equal(0.0, result.CurrentPowerKw);
        Assert.Equal(0.0, result.CapacityPercentage);
        Assert.False(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_ParkWithEquipmentAndNoPowerDuringDaylight_StillRaisesTheAlert()
    {
        var park = CreateActivePark(maxCapacityKw: 250, thresholdKw: 40);
        var midday = new DateTime(2026, 11, 12, 12, 30, 0);

        var result = _service.CalculateMetrics(park, currentPowerKw: 0, energyTodayKwh: 0, midday, hasEquipment: true);

        Assert.True(result.IsAlertActive);
    }

    [Fact]
    public void CalculateMetrics_TimestampIsTheOneGiven()
    {
        var park = CreateActivePark();
        var instant = new DateTime(2026, 11, 12, 15, 0, 5, DateTimeKind.Utc);

        var result = _service.CalculateMetrics(park, 300, 1000, new DateTime(2026, 11, 12, 12, 0, 5), timestampUtc: instant);

        Assert.Equal(instant, result.TimestampUtc);
    }
}
