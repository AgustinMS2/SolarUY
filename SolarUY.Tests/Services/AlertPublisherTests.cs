using Microsoft.Extensions.Logging.Abstractions;
using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Hubs;
using SolarUY.ApiService.Services;
using SolarUY.Tests.Fakes;
using Xunit;

namespace SolarUY.Tests.Services;

public class AlertPublisherTests
{
    private static readonly Guid ParkId = Guid.NewGuid();
    private static readonly Guid NodeId = Guid.NewGuid();
    private static readonly DateTimeOffset Start = new(2026, 11, 15, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeAlertRepository _repository = new();
    private readonly TestTimeProvider _clock = new(Start);
    private readonly FakeAlertsHubContext _hub = new();
    private readonly AlertService _alertService;
    private readonly AlertPublisher _publisher;

    public AlertPublisherTests()
    {
        _alertService = new AlertService(_repository, _clock);
        _publisher = new AlertPublisher(_alertService, _hub, NullLogger<AlertPublisher>.Instance);
    }

    private static FaultDetection Fault() =>
        new(ParkId, "Parque Salto", AlertEquipmentType.Node, NodeId, "Nodo Norte", AlertSeverity.Warning, "Temperatura alta en inversor (78.0 °C).");

    [Fact]
    public async Task PublishAsync_NewFault_EmitsANewAlertEventToTheAlertsGroup()
    {
        await _publisher.PublishAsync(Fault(), CancellationToken.None);

        var sent = Assert.Single(_hub.Sent);
        Assert.Equal(AlertsHub.GroupName, sent.Group);
        Assert.Equal(AlertsHub.ReceiveEventName, sent.Method);
        var alertEvent = Assert.IsType<AlertEventDto>(sent.Payload);
        Assert.Equal(AlertEventKind.New, alertEvent.Kind);
        Assert.Equal(AlertStatus.Pending, alertEvent.Alert.Status);
        Assert.Equal("Nodo Norte", alertEvent.Alert.EquipmentName);
        Assert.Equal(1, alertEvent.UnreadCount);
    }

    [Fact]
    public async Task PublishAsync_DuplicateWithinSixtySeconds_EmitsNothing()
    {
        await _publisher.PublishAsync(Fault(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(30));

        await _publisher.PublishAsync(Fault(), CancellationToken.None);

        Assert.Single(_hub.Sent);
        Assert.Single(_repository.Alerts);
    }

    [Fact]
    public async Task PublishAsync_ThirdDetectionInFiveMinutes_EmitsAnUpdatedEventWithTheInstanceCount()
    {
        await _publisher.PublishAsync(Fault(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(61));
        await _publisher.PublishAsync(Fault(), CancellationToken.None);
        _clock.Advance(TimeSpan.FromSeconds(61));

        await _publisher.PublishAsync(Fault(), CancellationToken.None);

        Assert.Equal(3, _hub.Sent.Count);
        var last = Assert.IsType<AlertEventDto>(_hub.Sent[^1].Payload);
        Assert.Equal(AlertEventKind.Updated, last.Kind);
        Assert.Equal(3, last.Alert.OccurrenceCount);
        Assert.Equal(1, last.UnreadCount);
    }

    [Fact]
    public async Task PublishAsync_WithNoFault_DoesNothing()
    {
        await _publisher.PublishAsync(null, CancellationToken.None);

        Assert.Empty(_hub.Sent);
        Assert.Empty(_repository.Alerts);
    }

    [Fact]
    public async Task PublishAsync_WhenRegisteringFails_DoesNotThrow()
    {
        var publisher = new AlertPublisher(new ThrowingAlertService(), _hub, NullLogger<AlertPublisher>.Instance);

        await publisher.PublishAsync(Fault(), CancellationToken.None);

        Assert.Empty(_hub.Sent);
    }

    [Fact]
    public async Task PublishStatusChangeAsync_EmitsAStatusChangedEventWithTheCurrentUnreadCount()
    {
        _alertService.Register(Fault());
        var alert = _alertService.Acknowledge(_repository.Alerts[0].Id).Value!;

        await _publisher.PublishStatusChangeAsync(alert, CancellationToken.None);

        var sent = Assert.Single(_hub.Sent);
        var alertEvent = Assert.IsType<AlertEventDto>(sent.Payload);
        Assert.Equal(AlertEventKind.StatusChanged, alertEvent.Kind);
        Assert.Equal(AlertStatus.Acknowledged, alertEvent.Alert.Status);
        Assert.Equal(0, alertEvent.UnreadCount);
    }

    private sealed class ThrowingAlertService : IAlertService
    {
        public AlertRegistration Register(FaultDetection fault) => throw new InvalidOperationException("base de datos no disponible");
        public ServiceResult<AlertDto> Acknowledge(Guid id) => throw new NotSupportedException();
        public ServiceResult<AlertDto> Dismiss(Guid id) => throw new NotSupportedException();
        public AlertListDto GetRecent(int take) => throw new NotSupportedException();
        public int GetUnreadCount() => throw new NotSupportedException();
        public int PurgeDismissed() => throw new NotSupportedException();
        public IReadOnlyList<AlertDto> AttendPanelAlerts(Guid panelId) => throw new NotSupportedException();
    }
}
