using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class PanelHealthServiceTests
{
    // Panel de 400 W: a las 12:45 (mediodía solar) la potencia esperada es exactamente 400 W.
    // Se usa el 30 de octubre (amanecer a las 06:00 y atardecer a las 19:30, spec 036).
    private const double Noon = 12.75;
    private const double Night = 23.0;

    private static DateTime Local(double hour) => new DateTime(2026, 10, 30).AddHours(hour);

    private readonly PanelHealthService _service = new(new PanelTelemetryService());

    private static SolarPanel CreatePanel(
        SolarPanelStatus status = SolarPanelStatus.Active,
        Guid? nodeId = null,
        bool withNode = true,
        int powerW = 400,
        double efficiency = 20.0)
    {
        return new SolarPanel
        {
            Id = Guid.NewGuid(),
            SerieNumber = "PNL-0001",
            NormalizedSerieNumber = "pnl-0001",
            PowerW = powerW,
            Efficiency = efficiency,
            SolarParkId = Guid.NewGuid(),
            SolarNodeId = withNode ? (nodeId ?? Guid.NewGuid()) : null,
            Status = status,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private PanelHealthReadingDto Classify(double activePowerW, double hour = Noon, SolarPanel? panel = null, bool hasActiveNode = true)
        => _service.ClassifyHealth(panel ?? CreatePanel(), hasActiveNode, activePowerW, Local(hour));

    // ---------- Clasificación por generación (con sol) ----------

    [Theory]
    [InlineData(400.0)]   // 100 %
    [InlineData(320.0)]   // exactamente 80 %
    public void ClassifyHealth_AtOrAboveEightyPercentOfExpected_IsNormal(double activePowerW)
    {
        Assert.Equal(PanelHealthState.Normal, Classify(activePowerW).State);
    }

    [Theory]
    [InlineData(319.0)]   // justo por debajo del 80 %
    [InlineData(200.0)]   // 50 %
    [InlineData(40.0)]    // exactamente 10 %
    public void ClassifyHealth_BetweenTenAndEightyPercent_IsDegraded(double activePowerW)
    {
        Assert.Equal(PanelHealthState.Degraded, Classify(activePowerW).State);
    }

    [Theory]
    [InlineData(39.0)]    // justo por debajo del 10 %
    [InlineData(5.0)]
    [InlineData(0.0)]
    public void ClassifyHealth_BelowTenPercentOfExpected_IsCritical(double activePowerW)
    {
        Assert.Equal(PanelHealthState.Critical, Classify(activePowerW).State);
    }

    [Fact]
    public void ClassifyHealth_WithNegativeReading_IsTreatedAsZeroAndCritical()
    {
        var reading = Classify(-12.0);

        Assert.Equal(PanelHealthState.Critical, reading.State);
        Assert.Equal(0.0, reading.ActivePowerW);
    }

    [Fact]
    public void ClassifyHealth_CopiesPanelIdAndReportsPowers()
    {
        var panel = CreatePanel();

        var reading = _service.ClassifyHealth(panel, true, 300.0, Local(Noon));

        Assert.Equal(panel.Id, reading.PanelId);
        Assert.Equal(300.0, reading.ActivePowerW);
        Assert.Equal(400.0, reading.ExpectedPowerW);
        Assert.Null(reading.StateReason);
    }

    // ---------- Rendimiento bajo (misma regla que la spec 029) ----------

    [Theory]
    [InlineData(159.0, true)]    // menos del 40 %
    [InlineData(160.0, false)]   // exactamente 40 %
    [InlineData(300.0, false)]
    public void ClassifyHealth_LowPerformanceIsBelowFortyPercentOfExpected(double activePowerW, bool expectedLow)
    {
        Assert.Equal(expectedLow, Classify(activePowerW).IsLowPerformance);
    }

    [Fact]
    public void ClassifyHealth_LowPerformancePanelBetweenTenAndFortyPercent_IsDegradedWithLowFlag()
    {
        var reading = Classify(100.0); // 25 %

        Assert.Equal(PanelHealthState.Degraded, reading.State);
        Assert.True(reading.IsLowPerformance);
    }

    // ---------- Eficiencia estimada ----------

    [Fact]
    public void ClassifyHealth_EstimatedEfficiencyScalesNameplateEfficiencyByActualOverExpected()
    {
        // 20 % × (300 / 400) = 15 %
        var reading = Classify(300.0, panel: CreatePanel(efficiency: 20.0));

        Assert.Equal(15.0, reading.EstimatedEfficiencyPercent);
    }

    // ---------- Sin sol (de noche) ----------

    [Fact]
    public void ClassifyHealth_AtNightWithActiveNode_IsNoSunAndNeverEvaluatesPerformance()
    {
        var reading = Classify(0.0, hour: Night);

        Assert.Equal(PanelHealthState.NoSun, reading.State);
        Assert.False(reading.IsLowPerformance);
        Assert.Null(reading.EstimatedEfficiencyPercent);
        Assert.Equal(0.0, reading.ExpectedPowerW);
        Assert.Equal(0.0, reading.ActivePowerW);
    }

    [Fact]
    public void ClassifyHealth_AtNightWithStrayReading_StillNoSunWithoutDivisionByZero()
    {
        var reading = Classify(7.0, hour: Night);

        Assert.Equal(PanelHealthState.NoSun, reading.State);
        Assert.Null(reading.EstimatedEfficiencyPercent);
    }

    // ---------- Época del año (spec 036) ----------

    [Fact]
    public void ClassifyHealth_InWinterAtHalfPastSixInTheMorning_IsNoSunBecauseTheSunHasNotRisen()
    {
        // 21 de junio: el sol sale a las 07:42.
        var reading = _service.ClassifyHealth(CreatePanel(), true, 0.0, new DateTime(2026, 6, 21, 6, 30, 0));

        Assert.Equal(PanelHealthState.NoSun, reading.State);
    }

    [Fact]
    public void ClassifyHealth_InSummerAtHalfPastSixInTheMorning_IsEvaluatedByGeneration()
    {
        // 21 de diciembre: el sol sale a las 05:36, por lo que un panel sin generar a esa hora ya es crítico.
        var reading = _service.ClassifyHealth(CreatePanel(), true, 0.0, new DateTime(2026, 12, 21, 6, 30, 0));

        Assert.Equal(PanelHealthState.Critical, reading.State);
        Assert.True(reading.ExpectedPowerW > 0.0);
    }

    // ---------- Inactivo / sin lecturas ----------

    [Fact]
    public void ClassifyHealth_InactivePanel_IsInactiveWithReason()
    {
        var reading = Classify(380.0, panel: CreatePanel(status: SolarPanelStatus.Inactive));

        Assert.Equal(PanelHealthState.Inactive, reading.State);
        Assert.Equal("Panel dado de baja", reading.StateReason);
        Assert.Null(reading.ActivePowerW);
        Assert.Null(reading.EstimatedEfficiencyPercent);
        Assert.False(reading.IsLowPerformance);
    }

    [Fact]
    public void ClassifyHealth_ActivePanelWithoutNode_IsInactiveWithReason()
    {
        var reading = Classify(380.0, panel: CreatePanel(withNode: false), hasActiveNode: false);

        Assert.Equal(PanelHealthState.Inactive, reading.State);
        Assert.Equal("Sin nodo asignado", reading.StateReason);
        Assert.Null(reading.ActivePowerW);
    }

    [Fact]
    public void ClassifyHealth_ActivePanelWithInactiveNode_IsInactiveWithReason()
    {
        var reading = Classify(380.0, panel: CreatePanel(), hasActiveNode: false);

        Assert.Equal(PanelHealthState.Inactive, reading.State);
        Assert.Equal("Nodo inactivo", reading.StateReason);
        Assert.Null(reading.ActivePowerW);
    }

    [Fact]
    public void ClassifyHealth_InactivePanelAtNight_IsStillInactiveNotNoSun()
    {
        var reading = Classify(0.0, hour: Night, panel: CreatePanel(status: SolarPanelStatus.Inactive));

        Assert.Equal(PanelHealthState.Inactive, reading.State);
    }

    [Fact]
    public void ClassifyHealth_WithNullPanel_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.ClassifyHealth(null!, true, 100, Local(Noon)));
    }

    [Fact]
    public void ClassifyHealth_PanelWithPendingMaintenanceIsStillClassifiedByGeneration()
    {
        // Una orden de mantenimiento no apaga el panel: la clasificación solo mira la generación.
        var reading = Classify(380.0);

        Assert.Equal(PanelHealthState.Normal, reading.State);
    }

    // ---------- Contadores ----------

    [Fact]
    public void BuildSummary_CountsTotalAndEachState()
    {
        var states = new[]
        {
            PanelHealthState.Normal, PanelHealthState.Normal, PanelHealthState.Normal,
            PanelHealthState.Degraded, PanelHealthState.Degraded,
            PanelHealthState.Critical,
            PanelHealthState.NoSun, PanelHealthState.NoSun,
            PanelHealthState.Inactive
        };

        var summary = _service.BuildSummary(states);

        Assert.Equal(9, summary.Total);
        Assert.Equal(3, summary.Normal);
        Assert.Equal(2, summary.Degraded);
        Assert.Equal(1, summary.Critical);
        Assert.Equal(2, summary.NoSun);
        Assert.Equal(1, summary.Inactive);
    }

    [Fact]
    public void BuildSummary_WithNoPanels_IsAllZeros()
    {
        var summary = _service.BuildSummary(Array.Empty<PanelHealthState>());

        Assert.Equal(0, summary.Total);
        Assert.Equal(0, summary.Normal);
        Assert.Equal(0, summary.Degraded);
        Assert.Equal(0, summary.Critical);
        Assert.Equal(0, summary.NoSun);
        Assert.Equal(0, summary.Inactive);
    }

    // ---------- Último mantenimiento ----------

    private static MaintenanceOrder Order(Guid panelId, MaintenanceStatus status, DateTime? completedAtUtc) => new()
    {
        Id = Guid.NewGuid(),
        SolarPanelId = panelId,
        TechnicianId = Guid.NewGuid(),
        Type = MaintenanceType.Preventive,
        ScheduledDateUtc = DateTime.UtcNow,
        Description = "Revisión",
        Status = status,
        CreatedAtUtc = DateTime.UtcNow,
        CompletedAtUtc = completedAtUtc
    };

    [Fact]
    public void LatestCompletedByPanel_TakesTheMostRecentCompletedOrder()
    {
        var panelId = Guid.NewGuid();
        var older = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 11, 3, 15, 30, 0, DateTimeKind.Utc);

        var result = _service.LatestCompletedByPanel(new[]
        {
            Order(panelId, MaintenanceStatus.Completed, older),
            Order(panelId, MaintenanceStatus.Completed, newer)
        });

        Assert.Equal(newer, result[panelId]);
    }

    [Fact]
    public void LatestCompletedByPanel_IgnoresPendingAndCancelledOrders()
    {
        var panelId = Guid.NewGuid();
        var completed = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

        var result = _service.LatestCompletedByPanel(new[]
        {
            Order(panelId, MaintenanceStatus.Completed, completed),
            Order(panelId, MaintenanceStatus.Pending, null),
            Order(panelId, MaintenanceStatus.Cancelled, DateTime.UtcNow)
        });

        Assert.Equal(completed, result[panelId]);
    }

    [Fact]
    public void LatestCompletedByPanel_PanelWithoutCompletedOrdersIsNotListed()
    {
        var panelId = Guid.NewGuid();

        var result = _service.LatestCompletedByPanel(new[] { Order(panelId, MaintenanceStatus.Pending, null) });

        Assert.False(result.ContainsKey(panelId));
    }

    [Fact]
    public void LatestCompletedByPanel_KeepsEachPanelSeparate()
    {
        var panelA = Guid.NewGuid();
        var panelB = Guid.NewGuid();
        var dateA = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
        var dateB = new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

        var result = _service.LatestCompletedByPanel(new[]
        {
            Order(panelA, MaintenanceStatus.Completed, dateA),
            Order(panelB, MaintenanceStatus.Completed, dateB)
        });

        Assert.Equal(dateA, result[panelA]);
        Assert.Equal(dateB, result[panelB]);
    }

    [Fact]
    public void LatestCompletedByPanel_WithNoOrders_IsEmpty()
    {
        Assert.Empty(_service.LatestCompletedByPanel(Array.Empty<MaintenanceOrder>()));
    }

    [Fact]
    public void ClassifyHealth_DeactivatedPanel_HasDeactivatedManuallyReason()
    {
        var reading = Classify(380.0, panel: CreatePanel(status: SolarPanelStatus.Deactivated));

        Assert.Equal(PanelHealthState.Inactive, reading.State);
        Assert.Equal("Panel desactivado manualmente", reading.StateReason);
        Assert.Null(reading.ActivePowerW);
    }
}
