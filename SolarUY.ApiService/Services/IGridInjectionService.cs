namespace SolarUY.ApiService.Services;

/// <summary>
/// Control maestro de la inyección a la red (spec 033): corte y reanudación administrativa
/// para un parque completo o para el inversor individual de un nodo.
/// </summary>
public interface IGridInjectionService
{
    /// <summary>RF-1/RF-2: corte de la inyección a la red de todo el parque (todos sus inversores).</summary>
    ServiceResult CutParkInjection(Guid parkId, string confirmationText, string orderedBy, string? remoteIp, string? reason);

    /// <summary>RF-5: reanudación de la inyección a la red de todo el parque.</summary>
    ServiceResult ResumeParkInjection(Guid parkId, string confirmationText, string orderedBy, string? remoteIp, string? reason);

    /// <summary>Corte de la inyección del inversor individual del nodo (caso límite).</summary>
    ServiceResult CutInverterInjection(Guid nodeId, string confirmationText, string orderedBy, string? remoteIp, string? reason);

    /// <summary>Reanudación de la inyección del inversor individual del nodo (caso límite).</summary>
    ServiceResult ResumeInverterInjection(Guid nodeId, string confirmationText, string orderedBy, string? remoteIp, string? reason);
}
