using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Registra una falla detectada y, si la alerta se creó o se consolidó, la emite por SignalR (Spec 031, RF-1 y RF-2).
/// Los duplicados no emiten nada.
/// </summary>
public interface IAlertPublisher
{
    /// <summary>No hace nada si <paramref name="fault"/> es nulo. Nunca lanza: un fallo de alertas no debe cortar la telemetría.</summary>
    Task PublishAsync(FaultDetection? fault, CancellationToken cancellationToken);

    /// <summary>Emite el cambio de estado de una alerta (reconocida o descartada) a todos los usuarios conectados.</summary>
    Task PublishStatusChangeAsync(AlertDto alert, CancellationToken cancellationToken);
}
