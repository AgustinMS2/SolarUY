namespace SolarUY.ApiService.Domain;

// Spec 033: bitácora inmutable de auditoría de órdenes de inyección a la red (solo altas, sin edición ni borrado).
public class GridInjectionEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public InjectionTarget TargetType { get; set; }

    // Id del parque (TargetType = Park) o del inversor (TargetType = Inverter).
    public Guid TargetId { get; set; }

    // Snapshot del nombre del parque o del número de serie del inversor al momento de la orden.
    public string TargetName { get; set; } = string.Empty;

    // Parque al que pertenece el objetivo (para objetivo inversor es su parque).
    public Guid ParkId { get; set; }

    public SolarPark? Park { get; set; }

    public InjectionAction Action { get; set; }

    // Usuario autenticado que ordenó la acción.
    public string OrderedBy { get; set; } = string.Empty;

    public DateTime OrderedAtUtc { get; set; }

    // IP de la petición HTTP recibida por la API (en el BFF actual, la del servidor Web).
    public string? RemoteIp { get; set; }

    // Motivo opcional del corte indicado por el Administrador.
    public string? Reason { get; set; }

    // Cantidad de órdenes confirmadas: 1 en corte por inversor, N en corte de parque, 0 si el objetivo no tenía inversor.
    public int OrdersSent { get; set; }
}

public enum InjectionTarget
{
    /// <summary>La orden afecta a todo el parque solar.</summary>
    Park = 1,

    /// <summary>La orden afecta al inversor individual del nodo.</summary>
    Inverter = 2
}

public enum InjectionAction
{
    /// <summary>Orden de corte / desacople de la inyección a la red (RF-2).</summary>
    Cut = 1,

    /// <summary>Orden de reanudación / sincronización con la red (RF-5).</summary>
    Resume = 2
}
