using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class PanelTelemetryServiceTests
{
    private readonly PanelTelemetryService _service = new();

    private static SolarPanel CreatePanel(int powerW = 400, double efficiency = 20.0)
    {
        return new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PNL-0001",
            NormalizedSerieNumber = "pnl-0001",
            PowerW = powerW,
            Efficiency = efficiency,
            SolarParkId = Guid.NewGuid(),
            SolarNodeId = Guid.NewGuid(),
            Status = SolarPanelStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    // ---------- Curva de potencia esperada (spec 029, métricas) ----------

    [Theory]
    [InlineData(0.0)]
    [InlineData(3.0)]
    [InlineData(5.99)]
    [InlineData(6.0)]
    [InlineData(20.0)]
    [InlineData(22.5)]
    [InlineData(23.99)]
    public void CalculateExpectedPowerW_OutsideDaylightHours_IsZero(double hour)
    {
        Assert.Equal(0.0, _service.CalculateExpectedPowerW(400, hour));
    }

    [Fact]
    public void CalculateExpectedPowerW_AtSolarNoon_IsTheNominalPower()
    {
        Assert.Equal(400.0, _service.CalculateExpectedPowerW(400, 13.0));
    }

    [Fact]
    public void CalculateExpectedPowerW_AtSevenInTheMorning_FollowsTheSineCurve()
    {
        // 400 × sen(π / 14) ≈ 89.0 W
        Assert.Equal(89.0, _service.CalculateExpectedPowerW(400, 7.0), precision: 1);
    }

    [Fact]
    public void CalculateExpectedPowerW_IsSymmetricAroundSolarNoon()
    {
        var morning = _service.CalculateExpectedPowerW(400, 9.0);
        var afternoon = _service.CalculateExpectedPowerW(400, 17.0);

        Assert.Equal(morning, afternoon);
        Assert.Equal(249.0, morning, precision: 0);
    }

    // ---------- CalculateMetrics ----------

    [Fact]
    public void CalculateMetrics_WithNullPanel_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _service.CalculateMetrics(null!, "Parque", "Nodo", 300, 13.0, DateTime.UtcNow));
    }

    [Fact]
    public void CalculateMetrics_CopiesPanelNameplateAndContextData()
    {
        var panel = CreatePanel(powerW: 450, efficiency: 21.5);
        var timestamp = new DateTime(2026, 11, 15, 16, 0, 0, DateTimeKind.Utc);

        var dto = _service.CalculateMetrics(panel, "Parque Salto", "Nodo Norte", 300, 13.0, timestamp);

        Assert.Equal(panel.Id, dto.PanelId);
        Assert.Equal("PNL-0001", dto.SerieNumber);
        Assert.Equal("Parque Salto", dto.ParkName);
        Assert.Equal("Nodo Norte", dto.NodeName);
        Assert.Equal(450, dto.NominalPowerW);
        Assert.Equal(21.5, dto.NominalEfficiencyPercent);
        Assert.Equal(timestamp, dto.TimestampUtc);
    }

    [Fact]
    public void CalculateMetrics_WithoutParkOrNodeName_UsesEmptyText()
    {
        var dto = _service.CalculateMetrics(CreatePanel(), null, null, 300, 13.0, DateTime.UtcNow);

        Assert.Equal(string.Empty, dto.ParkName);
        Assert.Equal(string.Empty, dto.NodeName);
    }

    [Fact]
    public void CalculateMetrics_ComputesPercentOfNominalAndExpectedPower()
    {
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 380, 13.0, DateTime.UtcNow);

        Assert.Equal(380.0, dto.ActivePowerW);
        Assert.Equal(400.0, dto.ExpectedPowerW);
        Assert.Equal(95.0, dto.PercentOfNominal);
    }

    [Fact]
    public void CalculateMetrics_EstimatedEfficiencyScalesNameplateEfficiencyByActualOverExpected()
    {
        // 20 % × (380 / 400) = 19 %
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400, efficiency: 20.0), "P", "N", 380, 13.0, DateTime.UtcNow);

        Assert.Equal(19.0, dto.EstimatedEfficiencyPercent);
    }

    [Fact]
    public void CalculateMetrics_AtNight_EstimatedEfficiencyIsNullAndThereIsNoDivisionByZero()
    {
        var dto = _service.CalculateMetrics(CreatePanel(), "P", "N", 0, 23.0, DateTime.UtcNow);

        Assert.Equal(0.0, dto.ExpectedPowerW);
        Assert.Null(dto.EstimatedEfficiencyPercent);
        Assert.Equal(0.0, dto.ActivePowerW);
        Assert.Equal(0.0, dto.PercentOfNominal);
    }

    [Fact]
    public void CalculateMetrics_WithNegativeReading_TreatsItAsZero()
    {
        var dto = _service.CalculateMetrics(CreatePanel(), "P", "N", -15, 13.0, DateTime.UtcNow);

        Assert.Equal(0.0, dto.ActivePowerW);
        Assert.Equal(0.0, dto.PercentOfNominal);
    }

    [Fact]
    public void CalculateMetrics_WithZeroNominalPower_DoesNotDivideByZero()
    {
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 0), "P", "N", 50, 13.0, DateTime.UtcNow);

        Assert.Equal(0.0, dto.PercentOfNominal);
        Assert.Null(dto.EstimatedEfficiencyPercent);
        Assert.False(dto.IsLowPerformance);
    }

    // ---------- Rendimiento bajo (RF-3) ----------

    [Fact]
    public void CalculateMetrics_BelowFortyPercentOfExpected_IsLowPerformance()
    {
        // Esperado al mediodía: 400 W → umbral 160 W.
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 159, 13.0, DateTime.UtcNow);

        Assert.True(dto.IsLowPerformance);
    }

    [Fact]
    public void CalculateMetrics_ExactlyFortyPercentOfExpected_IsNotLowPerformance()
    {
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 160, 13.0, DateTime.UtcNow);

        Assert.False(dto.IsLowPerformance);
    }

    [Fact]
    public void CalculateMetrics_AboveFortyPercentOfExpected_IsNotLowPerformance()
    {
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 380, 13.0, DateTime.UtcNow);

        Assert.False(dto.IsLowPerformance);
    }

    [Fact]
    public void CalculateMetrics_LowSunInTheMorningWithNormalOutput_IsNotLowPerformance()
    {
        // A las 7:00 se esperan ~89 W; producir 90 W es normal aunque sea menos del 40 % de la placa.
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 90, 7.0, DateTime.UtcNow);

        Assert.False(dto.IsLowPerformance);
    }

    [Fact]
    public void CalculateMetrics_LowSunInTheMorningWithPoorOutput_IsLowPerformance()
    {
        // A las 7:00 el umbral es ~35.6 W; 20 W queda por debajo.
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 20, 7.0, DateTime.UtcNow);

        Assert.True(dto.IsLowPerformance);
    }

    [Fact]
    public void CalculateMetrics_AtNight_NeverRaisesLowPerformance()
    {
        var dto = _service.CalculateMetrics(CreatePanel(powerW: 400), "P", "N", 0, 22.0, DateTime.UtcNow);

        Assert.False(dto.IsLowPerformance);
    }
}
