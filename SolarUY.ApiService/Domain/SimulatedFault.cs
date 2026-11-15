namespace SolarUY.ApiService.Domain;

/// <summary>Tipos de falla que se pueden simular (spec 037).</summary>
public enum SimulatedFaultType
{
    /// <summary>Panel: corte total (circuito abierto), la potencia cae a 0 W.</summary>
    OpenCircuit = 1,

    /// <summary>Panel: degradación severa o suciedad, la eficiencia cae al 30 % de la esperada.</summary>
    SevereDegradation = 2,

    /// <summary>Inversor: la temperatura interna sube por encima de 85 °C.</summary>
    InverterOverTemperature = 3,

    /// <summary>Inversor: se apaga por la protección de fuga a tierra.</summary>
    InsulationFault = 4
}

/// <summary>Tipo de equipo sobre el que se inyecta una falla simulada (spec 037).</summary>
public enum SimulatedFaultTarget
{
    Panel = 1,
    Inverter = 2
}

/// <summary>
/// Falla simulada inyectada en un panel o en el inversor de un nodo (spec 037). Es un dato de simulación con inicio y fin:
/// restablecer un equipo termina sus fallas (le pone fecha de fin) y no borra nada, de modo que la historia calculada con el
/// simulador (spec 036) siga coincidiendo.
/// </summary>
/// <remarks>
/// Sin llaves foráneas a propósito, como la historia de la spec 036: dar de baja un equipo no borra ni bloquea sus fallas.
/// </remarks>
public class SimulatedFault
{
    public Guid Id { get; set; }

    public SimulatedFaultType FaultType { get; set; }

    public SimulatedFaultTarget TargetType { get; set; }

    /// <summary>Id del panel o del inversor, según <see cref="TargetType"/>.</summary>
    public Guid TargetId { get; set; }

    /// <summary>Parque al que pertenecía el equipo al inyectar la falla.</summary>
    public Guid SolarParkId { get; set; }

    public DateTime StartedAtUtc { get; set; }

    /// <summary>Instante en que se restableció el equipo; nulo mientras la falla sigue activa.</summary>
    public DateTime? EndedAtUtc { get; set; }

    /// <summary>Usuario autenticado que inyectó la falla.</summary>
    public string StartedBy { get; set; } = string.Empty;
}
