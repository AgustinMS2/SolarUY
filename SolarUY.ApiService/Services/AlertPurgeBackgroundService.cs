using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano que purga una vez por hora las alertas Descartadas nocturnas de más de
/// 24 horas, liberando espacio en el historial (Spec 031, RF-5).
/// </summary>
public class AlertPurgeBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AlertPurgeBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public AlertPurgeBackgroundService(IServiceScopeFactory scopeFactory, ILogger<AlertPurgeBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AlertPurgeBackgroundService iniciado con intervalo de {Interval} h", _interval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var removed = scope.ServiceProvider.GetRequiredService<IAlertService>().PurgeDismissed();

                if (removed > 0)
                {
                    _logger.LogInformation("Se purgaron {Count} alertas descartadas", removed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error durante la purga de alertas descartadas");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("AlertPurgeBackgroundService detenido");
    }
}
