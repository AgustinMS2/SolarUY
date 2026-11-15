using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Inyección y remoción de fallas simuladas en paneles e inversores (spec 037). Las fallas son datos de simulación con
/// inicio y fin; el simulador de la spec 036 las aplica a las lecturas en vivo, a las alertas y a la historia.
/// </summary>
public interface ISimulationFaultService
{
    /// <summary>RF-1: inyecta una falla; devuelve el mensaje de éxito con la serie del equipo.</summary>
    ServiceResult<string> Inject(SimulatedFaultTarget targetType, Guid targetId, SimulatedFaultType faultType, string startedBy);

    /// <summary>RF-4: termina todas las fallas activas del equipo (sin fallas no es un error).</summary>
    ServiceResult<string> ClearTarget(SimulatedFaultTarget targetType, Guid targetId);

    /// <summary>RF-5: termina todas las fallas activas.</summary>
    ServiceResult<string> ClearAll();

    /// <summary>Equipos que emiten lecturas, con su falla vigente, para la pantalla.</summary>
    SimulationFaultOverviewDto GetOverview();
}
