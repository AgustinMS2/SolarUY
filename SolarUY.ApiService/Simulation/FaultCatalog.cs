using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Simulation;

/// <summary>
/// Reglas puras de cada tipo de falla simulable (spec 037): a qué equipo se aplica, su nivel de alerta y su nombre.
/// </summary>
public static class FaultCatalog
{
    private static readonly SimulatedFaultType[] PanelFaults = [SimulatedFaultType.OpenCircuit, SimulatedFaultType.SevereDegradation];
    private static readonly SimulatedFaultType[] InverterFaults = [SimulatedFaultType.InverterOverTemperature, SimulatedFaultType.InsulationFault];

    public static SimulatedFaultTarget TargetOf(SimulatedFaultType type) => type switch
    {
        SimulatedFaultType.OpenCircuit or SimulatedFaultType.SevereDegradation => SimulatedFaultTarget.Panel,
        SimulatedFaultType.InverterOverTemperature or SimulatedFaultType.InsulationFault => SimulatedFaultTarget.Inverter,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de falla desconocido.")
    };

    /// <summary>Crítico para el corte total y la falla de aislamiento; Advertencia para la degradación y la sobretemperatura.</summary>
    public static AlertSeverity SeverityOf(SimulatedFaultType type) => type switch
    {
        SimulatedFaultType.OpenCircuit or SimulatedFaultType.InsulationFault => AlertSeverity.Critical,
        SimulatedFaultType.SevereDegradation or SimulatedFaultType.InverterOverTemperature => AlertSeverity.Warning,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de falla desconocido.")
    };

    public static string LabelOf(SimulatedFaultType type) => type switch
    {
        SimulatedFaultType.OpenCircuit => "Corte total (circuito abierto)",
        SimulatedFaultType.SevereDegradation => "Degradación severa / suciedad",
        SimulatedFaultType.InverterOverTemperature => "Sobretemperatura de inversor",
        SimulatedFaultType.InsulationFault => "Falla de aislamiento",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Tipo de falla desconocido.")
    };

    /// <summary>Tipos de falla que se pueden inyectar en un equipo de ese tipo.</summary>
    public static IReadOnlyList<SimulatedFaultType> TypesFor(SimulatedFaultTarget target) => target switch
    {
        SimulatedFaultTarget.Panel => PanelFaults,
        SimulatedFaultTarget.Inverter => InverterFaults,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Tipo de equipo desconocido.")
    };
}
