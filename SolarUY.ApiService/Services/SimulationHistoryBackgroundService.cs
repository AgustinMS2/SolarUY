using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Servicio en segundo plano de la historia simulada (spec 036, RF-3 y RF-8): al arrancar completa la historia que falta
/// sin demorar el inicio de la API y, después, guarda cada minuto cerrado de cada inversor y cada hora cerrada de cada
/// parque. Si otra copia del servidor guarda lo mismo a la vez, el guardado idempotente evita duplicados.
/// </summary>
public class SimulationHistoryBackgroundService : BackgroundService
{
    // Margen para que el minuto que acaba de cerrar ya esté completo antes de guardarlo.
    private static readonly TimeSpan MinuteMargin = TimeSpan.FromSeconds(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<SimulationOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SimulationHistoryBackgroundService> _logger;

    public SimulationHistoryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<SimulationOptions> options,
        TimeProvider timeProvider,
        ILogger<SimulationHistoryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Simulación deshabilitada: no se guarda historia.");
            return;
        }

        // Cede el control para que el arranque de la API no espere al relleno inicial.
        await Task.Yield();

        _logger.LogInformation("SimulationHistoryBackgroundService iniciado (relleno inicial de {Days} días)", _options.Value.BackfillDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CatchUp(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al guardar la historia de la simulación; se reintenta en el próximo minuto");
            }

            try
            {
                await Task.Delay(DelayUntilNextMinute(), _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("SimulationHistoryBackgroundService detenido");
    }

    private void CatchUp(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimulationHistoryService>();

        var started = _timeProvider.GetTimestamp();
        var result = service.CatchUp(_timeProvider.GetUtcNow().UtcDateTime, _options.Value.BackfillDays, cancellationToken);

        if (result.MinuteRows > 0 || result.HourRows > 0)
        {
            _logger.LogInformation(
                "Historia de la simulación: {Minutes} registros por minuto y {Hours} por hora guardados en {Elapsed:F1}s",
                result.MinuteRows,
                result.HourRows,
                _timeProvider.GetElapsedTime(started).TotalSeconds);
        }
    }

    private TimeSpan DelayUntilNextMinute()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var nextMinute = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMinute), DateTimeKind.Utc).AddMinutes(1);
        return nextMinute + MinuteMargin - now;
    }
}
