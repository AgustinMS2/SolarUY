using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using Xunit;

namespace SolarUY.Tests.Services;

public class AlertNotificationGroupingTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 15, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ParkA = Guid.NewGuid();
    private static readonly Guid ParkB = Guid.NewGuid();

    private static AlertArrival Arrival(
        Guid parkId,
        string parkName,
        double secondsAgo = 1,
        AlertSeverity severity = AlertSeverity.Warning,
        string equipment = "Nodo X",
        Guid? alertId = null)
    {
        var alert = new AlertDto(
            alertId ?? Guid.NewGuid(), parkId, parkName, AlertEquipmentType.Node, Guid.NewGuid(), equipment,
            severity, AlertStatus.Pending, "Temperatura alta en inversor (78.0 °C).", 1, Now, Now, null);

        return new AlertArrival(alert, Now.AddSeconds(-secondsAgo));
    }

    private static List<AlertArrival> Many(Guid parkId, string parkName, int count, double firstSecondsAgo = 5)
        => Enumerable.Range(0, count)
            .Select(i => Arrival(parkId, parkName, firstSecondsAgo - (i * 0.1), equipment: $"Nodo {i}"))
            .ToList();

    [Fact]
    public void Group_WithNoArrivals_ReturnsNoToasts()
    {
        Assert.Empty(AlertNotificationGrouping.Group(Array.Empty<AlertArrival>(), Now));
    }

    [Fact]
    public void Group_SingleAlert_IsAnIndividualToastWithTheEquipmentAndDescription()
    {
        var arrival = Arrival(ParkA, "Parque Salto", equipment: "Nodo Norte");

        var toast = Assert.Single(AlertNotificationGrouping.Group([arrival], Now));

        Assert.Equal(arrival.Alert.Id, toast.AlertId);
        Assert.Equal(ParkA, toast.ParkId);
        Assert.Equal("Parque Salto", toast.ParkName);
        Assert.Equal(AlertSeverity.Warning, toast.Severity);
        Assert.Equal(1, toast.Count);
        Assert.Contains("Nodo Norte", toast.Message);
        Assert.Contains("Temperatura alta en inversor", toast.Message);
    }

    [Fact]
    public void Group_FourAlertsFromTheSamePark_StayAsFourIndividualToasts()
    {
        var toasts = AlertNotificationGrouping.Group(Many(ParkA, "Parque Salto", 4), Now);

        Assert.Equal(4, toasts.Count);
        Assert.All(toasts, t => Assert.NotNull(t.AlertId));
    }

    [Fact]
    public void Group_FiveAlertsFromTheSamePark_BecomeASingleGroupedToast()
    {
        var toasts = AlertNotificationGrouping.Group(Many(ParkA, "Parque Salto", 5), Now);

        var toast = Assert.Single(toasts);
        Assert.Null(toast.AlertId);
        Assert.Equal(5, toast.Count);
        Assert.Equal(ParkA, toast.ParkId);
        Assert.Equal("Se detectaron 5 fallas simultáneas en Parque Salto", toast.Message);
    }

    [Fact]
    public void Group_FifteenAlerts_ReportTheTotalInTheMessage()
    {
        var toast = Assert.Single(AlertNotificationGrouping.Group(Many(ParkA, "Parque Salto", 15), Now));

        Assert.Equal(15, toast.Count);
        Assert.Equal("Se detectaron 15 fallas simultáneas en Parque Salto", toast.Message);
    }

    [Fact]
    public void Group_AlertsFromDifferentParksAreNeverMixed()
    {
        var arrivals = Many(ParkA, "Parque Salto", 3).Concat(Many(ParkB, "Parque Sur", 3)).ToList();

        var toasts = AlertNotificationGrouping.Group(arrivals, Now);

        Assert.Equal(6, toasts.Count);
        Assert.All(toasts, t => Assert.NotNull(t.AlertId));
    }

    [Fact]
    public void Group_OnlyTheParkWithABurstIsGrouped()
    {
        var arrivals = Many(ParkA, "Parque Salto", 5, firstSecondsAgo: 5)
            .Concat(Many(ParkB, "Parque Sur", 2, firstSecondsAgo: 4))
            .ToList();

        var toasts = AlertNotificationGrouping.Group(arrivals, Now);

        Assert.Equal(3, toasts.Count);
        Assert.Single(toasts, t => t.AlertId is null && t.ParkId == ParkA && t.Count == 5);
        Assert.Equal(2, toasts.Count(t => t.AlertId is not null && t.ParkId == ParkB));
    }

    [Fact]
    public void Group_ToastsAreOrderedByArrival()
    {
        var first = Arrival(ParkB, "Parque Sur", secondsAgo: 8, equipment: "Primero");
        var second = Arrival(ParkB, "Parque Sur", secondsAgo: 2, equipment: "Segundo");

        var toasts = AlertNotificationGrouping.Group([second, first], Now);

        Assert.Contains("Primero", toasts[0].Message);
        Assert.Contains("Segundo", toasts[1].Message);
    }

    [Fact]
    public void Group_IgnoresArrivalsOlderThanTheTenSecondWindow()
    {
        // 3 recientes y 3 de hace más de 10 s: no llegan a 5 dentro de la ventana.
        var arrivals = Many(ParkA, "Parque Salto", 3, firstSecondsAgo: 5)
            .Concat(Many(ParkA, "Parque Salto", 3, firstSecondsAgo: 30))
            .ToList();

        var toasts = AlertNotificationGrouping.Group(arrivals, Now);

        Assert.Equal(3, toasts.Count);
        Assert.All(toasts, t => Assert.NotNull(t.AlertId));
    }

    [Fact]
    public void Group_AnArrivalExactlyTenSecondsOldStillCounts_AndElevenDoesNot()
    {
        var inside = Many(ParkA, "Parque Salto", 4, firstSecondsAgo: 3).Append(Arrival(ParkA, "Parque Salto", secondsAgo: 10)).ToList();
        var outside = Many(ParkA, "Parque Salto", 4, firstSecondsAgo: 3).Append(Arrival(ParkA, "Parque Salto", secondsAgo: 11)).ToList();

        Assert.Single(AlertNotificationGrouping.Group(inside, Now));
        Assert.Equal(4, AlertNotificationGrouping.Group(outside, Now).Count);
    }

    [Fact]
    public void Group_TheSameAlertArrivingTwiceCountsOnce()
    {
        var repeatedId = Guid.NewGuid();
        var arrivals = Many(ParkA, "Parque Salto", 4)
            .Append(Arrival(ParkA, "Parque Salto", secondsAgo: 6, alertId: repeatedId))
            .Append(Arrival(ParkA, "Parque Salto", secondsAgo: 2, alertId: repeatedId))
            .ToList();

        var toasts = AlertNotificationGrouping.Group(arrivals, Now);

        // 4 distintas + 1 repetida = 5 alertas distintas dentro de la ventana: ráfaga.
        var toast = Assert.Single(toasts);
        Assert.Equal(5, toast.Count);
    }

    [Fact]
    public void Group_FourDistinctAlertsWithOneRepeated_DoNotFormABurst()
    {
        var repeatedId = Guid.NewGuid();
        var arrivals = Many(ParkA, "Parque Salto", 3)
            .Append(Arrival(ParkA, "Parque Salto", secondsAgo: 6, alertId: repeatedId))
            .Append(Arrival(ParkA, "Parque Salto", secondsAgo: 2, alertId: repeatedId))
            .ToList();

        var toasts = AlertNotificationGrouping.Group(arrivals, Now);

        Assert.Equal(4, toasts.Count);
    }

    [Fact]
    public void Group_BurstSeverityIsTheHighestOfItsAlerts()
    {
        var arrivals = Many(ParkA, "Parque Salto", 4)
            .Append(Arrival(ParkA, "Parque Salto", secondsAgo: 1, severity: AlertSeverity.Critical))
            .ToList();

        var toast = Assert.Single(AlertNotificationGrouping.Group(arrivals, Now));

        Assert.Equal(AlertSeverity.Critical, toast.Severity);
    }

    [Fact]
    public void Group_MixedSeveritiesOfTheSameParkCountTogether()
    {
        var arrivals = new List<AlertArrival>
        {
            Arrival(ParkA, "Parque Salto", 5, AlertSeverity.Warning),
            Arrival(ParkA, "Parque Salto", 4, AlertSeverity.Critical),
            Arrival(ParkA, "Parque Salto", 3, AlertSeverity.Informational),
            Arrival(ParkA, "Parque Salto", 2, AlertSeverity.Warning),
            Arrival(ParkA, "Parque Salto", 1, AlertSeverity.Critical)
        };

        var toast = Assert.Single(AlertNotificationGrouping.Group(arrivals, Now));

        Assert.Equal(5, toast.Count);
    }
}
