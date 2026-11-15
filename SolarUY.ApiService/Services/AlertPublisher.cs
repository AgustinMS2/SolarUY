using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Hubs;

namespace SolarUY.ApiService.Services;

public class AlertPublisher : IAlertPublisher
{
    private readonly IAlertService _alertService;
    private readonly IHubContext<AlertsHub> _hubContext;
    private readonly ILogger<AlertPublisher> _logger;

    public AlertPublisher(IAlertService alertService, IHubContext<AlertsHub> hubContext, ILogger<AlertPublisher> logger)
    {
        _alertService = alertService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PublishAsync(FaultDetection? fault, CancellationToken cancellationToken)
    {
        if (fault is null)
        {
            return;
        }

        try
        {
            var registration = _alertService.Register(fault);

            // RF-2: un duplicado dentro de la ventana de 60 s no genera ningún evento.
            if (registration.Outcome == AlertRegistrationOutcome.Discarded || registration.Alert is null)
            {
                return;
            }

            var kind = registration.Outcome == AlertRegistrationOutcome.Created
                ? AlertEventKind.New
                : AlertEventKind.Updated;

            await Send(new AlertEventDto(kind, registration.Alert, _alertService.GetUnreadCount()), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo registrar o emitir la alerta de {Equipment}", fault.EquipmentName);
        }
    }

    public async Task PublishStatusChangeAsync(AlertDto alert, CancellationToken cancellationToken)
    {
        try
        {
            await Send(new AlertEventDto(AlertEventKind.StatusChanged, alert, _alertService.GetUnreadCount()), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo emitir el cambio de estado de la alerta {AlertId}", alert.Id);
        }
    }

    private Task Send(AlertEventDto alertEvent, CancellationToken cancellationToken)
    {
        return _hubContext.Clients
            .Group(AlertsHub.GroupName)
            .SendAsync(AlertsHub.ReceiveEventName, alertEvent, cancellationToken);
    }
}
