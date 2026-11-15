using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class AlertServiceTests
{
    private static readonly Guid ParkId = Guid.NewGuid();
    private static readonly Guid NodeId = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 11, 15, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeAlertRepository _repository = new();
    private readonly TestTimeProvider _clock = new(Start);
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _service = new AlertService(_repository, _clock);
    }

    private static FaultDetection Fault(
        AlertSeverity severity = AlertSeverity.Warning,
        AlertEquipmentType type = AlertEquipmentType.Node,
        Guid? equipmentId = null,
        string description = "Temperatura alta en inversor (78.0 °C)")
        => new(ParkId, "Parque Salto", type, equipmentId ?? NodeId, "Nodo Norte", severity, description);

    private static Alert StoredAlert(
        AlertStatus status,
        DateTime createdAtUtc,
        AlertSeverity severity = AlertSeverity.Warning,
        AlertEquipmentType type = AlertEquipmentType.Node,
        Guid? equipmentId = null,
        int count = 1)
        => new()
        {
            Id = Guid.NewGuid(),
            SolarParkId = ParkId,
            ParkName = "Parque Salto",
            EquipmentType = type,
            EquipmentId = equipmentId ?? NodeId,
            EquipmentName = "Nodo Norte",
            Severity = severity,
            Status = status,
            Description = "Falla",
            OccurrenceCount = count,
            CreatedAtUtc = createdAtUtc,
            LastOccurrenceAtUtc = createdAtUtc
        };

    // =====================================================================
    // T2: registro, deduplicado y consolidación (RF-1, RF-2, RF-6)
    // =====================================================================

    [Fact]
    public void Register_NewFault_CreatesPendingAlertWithTheDetectionData()
    {
        var result = _service.Register(Fault());

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        var alert = Assert.Single(_repository.Alerts);
        Assert.NotEqual(Guid.Empty, alert.Id);
        Assert.Equal(AlertStatus.Pending, alert.Status);
        Assert.Equal(1, alert.OccurrenceCount);
        Assert.Equal(ParkId, alert.SolarParkId);
        Assert.Equal("Parque Salto", alert.ParkName);
        Assert.Equal(AlertEquipmentType.Node, alert.EquipmentType);
        Assert.Equal(NodeId, alert.EquipmentId);
        Assert.Equal("Nodo Norte", alert.EquipmentName);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
        Assert.Equal("Temperatura alta en inversor (78.0 °C)", alert.Description);
        Assert.Equal(Start.UtcDateTime, alert.CreatedAtUtc);
        Assert.Equal(Start.UtcDateTime, alert.LastOccurrenceAtUtc);
        Assert.Equal(DateTimeKind.Utc, alert.CreatedAtUtc.Kind);
    }

    [Fact]
    public void Register_NewFault_ReturnsTheCreatedAlertAsDto()
    {
        var result = _service.Register(Fault());

        Assert.NotNull(result.Alert);
        Assert.Equal(_repository.Alerts[0].Id, result.Alert!.Id);
        Assert.Equal(AlertStatus.Pending, result.Alert.Status);
        Assert.Equal("Nodo Norte", result.Alert.EquipmentName);
    }

    [Fact]
    public void Register_SameFaultWithinSixtySeconds_IsDiscardedWithoutCreatingAnything()
    {
        _service.Register(Fault());
        _clock.Advance(TimeSpan.FromSeconds(59));

        var result = _service.Register(Fault());

        Assert.Equal(AlertRegistrationOutcome.Discarded, result.Outcome);
        Assert.Null(result.Alert);
        Assert.Single(_repository.Alerts);
    }

    [Fact]
    public void Register_SameFaultAtExactlySixtySeconds_CreatesANewAlert()
    {
        _service.Register(Fault());
        _clock.Advance(TimeSpan.FromSeconds(60));

        var result = _service.Register(Fault());

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(2, _repository.Alerts.Count);
    }

    [Fact]
    public void Register_SameFaultAfterSixtyOneSeconds_CreatesANewAlert()
    {
        _service.Register(Fault());
        _clock.Advance(TimeSpan.FromSeconds(61));

        var result = _service.Register(Fault());

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(2, _repository.Alerts.Count);
    }

    [Theory]
    [InlineData(AlertStatus.Pending)]
    [InlineData(AlertStatus.Acknowledged)]
    [InlineData(AlertStatus.Dismissed)]
    [InlineData(AlertStatus.Attended)]
    public void Register_DuplicateIsDiscardedWhateverTheStatusOfTheExistingAlert(AlertStatus status)
    {
        _repository.Alerts.Add(StoredAlert(status, Start.UtcDateTime.AddSeconds(-30)));

        var result = _service.Register(Fault());

        Assert.Equal(AlertRegistrationOutcome.Discarded, result.Outcome);
        Assert.Single(_repository.Alerts);
    }

    [Fact]
    public void Register_SameEquipmentWithDifferentSeverity_IsNotADuplicate()
    {
        _service.Register(Fault(AlertSeverity.Warning));

        var result = _service.Register(Fault(AlertSeverity.Critical));

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(2, _repository.Alerts.Count);
    }

    [Fact]
    public void Register_SameSeverityOnDifferentEquipment_IsNotADuplicate()
    {
        _service.Register(Fault());

        var result = _service.Register(Fault(equipmentId: Guid.NewGuid()));

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(2, _repository.Alerts.Count);
    }

    [Fact]
    public void Register_SameIdentifierButDifferentEquipmentType_IsNotADuplicate()
    {
        _service.Register(Fault(type: AlertEquipmentType.Node));

        var result = _service.Register(Fault(type: AlertEquipmentType.Panel));

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(2, _repository.Alerts.Count);
    }

    [Fact]
    public void Register_ThirdDetectionWithinFiveMinutes_MergesIntoOneConsolidatedAlert()
    {
        _service.Register(Fault());                  // t = 0
        _clock.Advance(TimeSpan.FromSeconds(61));
        _service.Register(Fault());                  // t = 61
        _clock.Advance(TimeSpan.FromSeconds(61));

        var result = _service.Register(Fault());     // t = 122: la tercera

        Assert.Equal(AlertRegistrationOutcome.Consolidated, result.Outcome);
        var alert = Assert.Single(_repository.Alerts);
        Assert.Equal(3, alert.OccurrenceCount);
        Assert.Equal(Start.UtcDateTime, alert.CreatedAtUtc);
        Assert.Equal(Start.UtcDateTime.AddSeconds(122), alert.LastOccurrenceAtUtc);
        Assert.Equal(3, result.Alert!.OccurrenceCount);
    }

    [Fact]
    public void Register_DetectionsAfterConsolidationIncrementTheCounter()
    {
        _service.Register(Fault());
        _clock.Advance(TimeSpan.FromSeconds(61));
        _service.Register(Fault());
        _clock.Advance(TimeSpan.FromSeconds(61));
        _service.Register(Fault());                  // consolidada, cuenta 3
        _clock.Advance(TimeSpan.FromSeconds(61));

        var result = _service.Register(Fault());     // cuarta

        Assert.Equal(AlertRegistrationOutcome.Consolidated, result.Outcome);
        var alert = Assert.Single(_repository.Alerts);
        Assert.Equal(4, alert.OccurrenceCount);
        Assert.Equal(Start.UtcDateTime.AddSeconds(183), alert.LastOccurrenceAtUtc);
    }

    [Fact]
    public void Register_ThirdDetectionOutsideTheFiveMinuteWindow_DoesNotConsolidate()
    {
        _service.Register(Fault());                  // t = 0
        _clock.Advance(TimeSpan.FromSeconds(120));
        _service.Register(Fault());                  // t = 120
        _clock.Advance(TimeSpan.FromSeconds(210));

        var result = _service.Register(Fault());     // t = 330: la primera ya quedó fuera de los 5 minutos

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(3, _repository.Alerts.Count);
        Assert.All(_repository.Alerts, a => Assert.Equal(1, a.OccurrenceCount));
    }

    [Fact]
    public void Register_ConsolidationKeepsTheAlertPendingWhenAnyMergedAlertWasPending()
    {
        var first = StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime.AddSeconds(-200));
        first.AcknowledgedAtUtc = Start.UtcDateTime.AddSeconds(-150);
        _repository.Alerts.Add(first);
        _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddSeconds(-100)));

        _service.Register(Fault());

        var alert = Assert.Single(_repository.Alerts);
        Assert.Equal(3, alert.OccurrenceCount);
        Assert.Equal(AlertStatus.Pending, alert.Status);
        Assert.Null(alert.AcknowledgedAtUtc);
    }

    [Fact]
    public void Register_ConsolidationOfAcknowledgedAlertsKeepsThemAcknowledged()
    {
        _repository.Alerts.Add(StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime.AddSeconds(-200)));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime.AddSeconds(-100)));

        _service.Register(Fault());

        var alert = Assert.Single(_repository.Alerts);
        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
    }

    [Fact]
    public void Register_ConsolidationIgnoresDismissedAndAttendedAlerts()
    {
        _repository.Alerts.Add(StoredAlert(AlertStatus.Dismissed, Start.UtcDateTime.AddSeconds(-200)));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Attended, Start.UtcDateTime.AddSeconds(-100)));

        var result = _service.Register(Fault());

        Assert.Equal(AlertRegistrationOutcome.Created, result.Outcome);
        Assert.Equal(3, _repository.Alerts.Count);
    }

    [Fact]
    public void Register_ConsolidationDoesNotTouchAlertsOfOtherEquipment()
    {
        var otherNode = Guid.NewGuid();
        var other = StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddSeconds(-100), equipmentId: otherNode);
        _repository.Alerts.Add(other);
        _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddSeconds(-200)));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddSeconds(-120)));

        _service.Register(Fault());

        Assert.Equal(2, _repository.Alerts.Count);
        Assert.Contains(_repository.Alerts, a => a.Id == other.Id && a.OccurrenceCount == 1);
        Assert.Contains(_repository.Alerts, a => a.EquipmentId == NodeId && a.OccurrenceCount == 3);
    }

    // =====================================================================
    // T3: contador, reconocer, descartar y purga (RF-3, RF-4, RF-5, RF-7)
    // =====================================================================

    [Fact]
    public void GetUnreadCount_CountsOnlyPendingAlerts()
    {
        _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Dismissed, Start.UtcDateTime));
        _repository.Alerts.Add(StoredAlert(AlertStatus.Attended, Start.UtcDateTime));

        Assert.Equal(2, _service.GetUnreadCount());
    }

    [Fact]
    public void GetUnreadCount_CountsAFreshAlertImmediately()
    {
        _service.Register(Fault());

        Assert.Equal(1, _service.GetUnreadCount());
    }

    [Fact]
    public void GetRecent_ReturnsTheNewestFirstAndTheUnreadCount()
    {
        _repository.Alerts.Add(StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime.AddMinutes(-30)));
        var newest = StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddMinutes(-1));
        _repository.Alerts.Add(newest);
        _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddMinutes(-10)));

        var list = _service.GetRecent(50);

        Assert.Equal(2, list.UnreadCount);
        Assert.Equal(3, list.Alerts.Count);
        Assert.Equal(newest.Id, list.Alerts[0].Id);
        Assert.True(list.Alerts[0].LastOccurrenceAtUtc >= list.Alerts[1].LastOccurrenceAtUtc);
    }

    [Fact]
    public void GetRecent_RespectsTheRequestedLimit()
    {
        for (var i = 0; i < 5; i++)
        {
            _repository.Alerts.Add(StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddMinutes(-i)));
        }

        var list = _service.GetRecent(3);

        Assert.Equal(3, list.Alerts.Count);
        Assert.Equal(5, list.UnreadCount);
    }

    [Fact]
    public void Acknowledge_PendingAlert_BecomesAcknowledgedAndStopsCounting()
    {
        var alert = StoredAlert(AlertStatus.Pending, Start.UtcDateTime.AddMinutes(-1));
        _repository.Alerts.Add(alert);

        var result = _service.Acknowledge(alert.Id);

        Assert.True(result.Success);
        Assert.Equal(AlertStatus.Acknowledged, result.Value!.Status);
        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
        Assert.Equal(Start.UtcDateTime, alert.AcknowledgedAtUtc);
        Assert.Equal(0, _service.GetUnreadCount());
    }

    [Fact]
    public void Acknowledge_AlreadyAcknowledgedAlert_ChangesNothingAndDoesNotFail()
    {
        var firstTime = Start.UtcDateTime.AddMinutes(-5);
        var alert = StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime.AddMinutes(-10));
        alert.AcknowledgedAtUtc = firstTime;
        _repository.Alerts.Add(alert);

        var result = _service.Acknowledge(alert.Id);

        Assert.True(result.Success);
        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
        Assert.Equal(firstTime, alert.AcknowledgedAtUtc);
    }

    [Theory]
    [InlineData(AlertStatus.Dismissed)]
    [InlineData(AlertStatus.Attended)]
    public void Acknowledge_AlertPastAcknowledged_KeepsItsStatus(AlertStatus status)
    {
        var alert = StoredAlert(status, Start.UtcDateTime.AddMinutes(-10));
        _repository.Alerts.Add(alert);

        var result = _service.Acknowledge(alert.Id);

        Assert.True(result.Success);
        Assert.Equal(status, alert.Status);
        Assert.Null(alert.AcknowledgedAtUtc);
    }

    [Fact]
    public void Acknowledge_UnknownAlert_ReturnsNotFound()
    {
        var result = _service.Acknowledge(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("La alerta solicitada no existe.", result.ErrorMessage);
    }

    [Fact]
    public void Dismiss_AcknowledgedAlert_BecomesDismissed()
    {
        var alert = StoredAlert(AlertStatus.Acknowledged, Start.UtcDateTime.AddMinutes(-10));
        _repository.Alerts.Add(alert);

        var result = _service.Dismiss(alert.Id);

        Assert.True(result.Success);
        Assert.Equal(AlertStatus.Dismissed, result.Value!.Status);
        Assert.Equal(AlertStatus.Dismissed, alert.Status);
        Assert.Equal(Start.UtcDateTime, alert.DismissedAtUtc);
    }

    [Theory]
    [InlineData(AlertStatus.Pending)]
    [InlineData(AlertStatus.Dismissed)]
    [InlineData(AlertStatus.Attended)]
    public void Dismiss_AlertThatIsNotAcknowledged_ReturnsConflictAndChangesNothing(AlertStatus status)
    {
        var alert = StoredAlert(status, Start.UtcDateTime.AddMinutes(-10));
        _repository.Alerts.Add(alert);

        var result = _service.Dismiss(alert.Id);

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.Conflict, result.ErrorType);
        Assert.Equal("Solo se pueden descartar alertas reconocidas.", result.ErrorMessage);
        Assert.Equal(status, alert.Status);
        Assert.Null(alert.DismissedAtUtc);
    }

    [Fact]
    public void Dismiss_UnknownAlert_ReturnsNotFound()
    {
        var result = _service.Dismiss(Guid.NewGuid());

        Assert.False(result.Success);
        Assert.Equal(ServiceErrorType.NotFound, result.ErrorType);
        Assert.Equal("La alerta solicitada no existe.", result.ErrorMessage);
    }

    // ---------- Purga (RF-5) ----------
    // Reloj: 2026-11-15 15:00 UTC. Hora de Uruguay = UTC − 3 (nocturno: 00:00 a 06:00 locales = 03:00 a 09:00 UTC).

    private Alert Dismissed(DateTime createdAtUtc)
    {
        var alert = StoredAlert(AlertStatus.Dismissed, createdAtUtc);
        _repository.Alerts.Add(alert);
        return alert;
    }

    [Fact]
    public void PurgeDismissed_RemovesNocturnalDismissedAlertsOlderThanTwentyFourHours()
    {
        var old = Dismissed(new DateTime(2026, 11, 13, 4, 0, 0, DateTimeKind.Utc)); // 01:00 en Uruguay

        var removed = _service.PurgeDismissed();

        Assert.Equal(1, removed);
        Assert.DoesNotContain(_repository.Alerts, a => a.Id == old.Id);
    }

    [Fact]
    public void PurgeDismissed_KeepsDaytimeDismissedAlerts()
    {
        var day = Dismissed(new DateTime(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc)); // 12:00 en Uruguay

        var removed = _service.PurgeDismissed();

        Assert.Equal(0, removed);
        Assert.Contains(_repository.Alerts, a => a.Id == day.Id);
    }

    [Fact]
    public void PurgeDismissed_KeepsNocturnalAlertsYoungerThanTwentyFourHours()
    {
        var recent = Dismissed(new DateTime(2026, 11, 15, 6, 0, 0, DateTimeKind.Utc)); // 03:00 en Uruguay, hace 9 h

        Assert.Equal(0, _service.PurgeDismissed());
        Assert.Contains(_repository.Alerts, a => a.Id == recent.Id);
    }

    [Fact]
    public void PurgeDismissed_AlertCreatedExactlyTwentyFourHoursAgo_IsKept()
    {
        _clock.Now = new DateTimeOffset(2026, 11, 15, 4, 0, 0, TimeSpan.Zero);
        var exactly = Dismissed(new DateTime(2026, 11, 14, 4, 0, 0, DateTimeKind.Utc)); // 01:00 en Uruguay, exactamente 24 h

        Assert.Equal(0, _service.PurgeDismissed());
        Assert.Contains(_repository.Alerts, a => a.Id == exactly.Id);
    }

    [Theory]
    [InlineData(3, 0, true)]    // 00:00 en Uruguay: nocturna
    [InlineData(8, 59, true)]   // 05:59 en Uruguay: nocturna
    [InlineData(9, 0, false)]   // 06:00 en Uruguay: ya no es nocturna
    [InlineData(2, 59, false)]  // 23:59 del día anterior en Uruguay
    public void PurgeDismissed_NightWindowIsMidnightToSixLocalTime(int utcHour, int utcMinute, bool purged)
    {
        var alert = Dismissed(new DateTime(2026, 11, 12, utcHour, utcMinute, 0, DateTimeKind.Utc));

        var removed = _service.PurgeDismissed();

        Assert.Equal(purged ? 1 : 0, removed);
        Assert.Equal(!purged, _repository.Alerts.Any(a => a.Id == alert.Id));
    }

    [Theory]
    [InlineData(AlertStatus.Pending)]
    [InlineData(AlertStatus.Acknowledged)]
    [InlineData(AlertStatus.Attended)]
    public void PurgeDismissed_NeverRemovesAlertsThatAreNotDismissed(AlertStatus status)
    {
        var alert = StoredAlert(status, new DateTime(2026, 11, 13, 4, 0, 0, DateTimeKind.Utc));
        _repository.Alerts.Add(alert);

        Assert.Equal(0, _service.PurgeDismissed());
        Assert.Contains(_repository.Alerts, a => a.Id == alert.Id);
    }

    [Fact]
    public void PurgeDismissed_ReturnsHowManyAlertsItRemoved()
    {
        Dismissed(new DateTime(2026, 11, 12, 4, 0, 0, DateTimeKind.Utc));
        Dismissed(new DateTime(2026, 11, 13, 5, 0, 0, DateTimeKind.Utc));
        var kept = Dismissed(new DateTime(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc));

        var removed = _service.PurgeDismissed();

        Assert.Equal(2, removed);
        Assert.Equal(kept.Id, Assert.Single(_repository.Alerts).Id);
    }
}
