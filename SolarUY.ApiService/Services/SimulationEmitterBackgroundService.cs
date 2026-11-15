using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Base de los servicios que emiten lecturas en vivo por SignalR (specs 027 a 030, con el motor de la spec 036):
/// todos despiertan en los mismos instantes, múltiplos del intervalo configurado, y calculan sobre ese mismo instante,
/// por lo que las pantallas de parque, nodo, panel y salud muestran los mismos números. Se detienen si
/// <c>Simulation:Enabled</c> es falso.
/// </summary>
public abstract class SimulationEmitterBackgroundService : BackgroundService
{
    private readonly IOptions<SimulationOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    protected SimulationEmitterBackgroundService(IOptions<SimulationOptions> options, TimeProvider timeProvider, ILogger logger)
    {
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected abstract string ServiceName { get; }

    /// <summary>Emite un ciclo de lecturas calculadas para el instante dado (ya redondeado al intervalo).</summary>
    protected abstract Task EmitCycleAsync(DateTime instantUtc, CancellationToken cancellationToken);

    protected int IntervalSeconds => Math.Max(1, _options.Value.IntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("{Service}: simulación deshabilitada, no se emiten lecturas.", ServiceName);
            return;
        }

        _logger.LogInformation("{Service} iniciado con intervalo de {Interval}s", ServiceName, IntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var instant = SimulationClock.Snap(_timeProvider.GetUtcNow().UtcDateTime, IntervalSeconds);
                await EmitCycleAsync(instant, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error durante el ciclo de emisión de {Service}", ServiceName);
            }

            try
            {
                var delay = SimulationClock.DelayUntilNextCycle(_timeProvider.GetUtcNow().UtcDateTime, IntervalSeconds);
                await Task.Delay(delay, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("{Service} detenido", ServiceName);
    }
}
