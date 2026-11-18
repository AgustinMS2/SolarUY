using System.Text.Json.Serialization;
using SolarUY.ApiService.Domain;

namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Petición para inyectar una falla simulada (spec 037). <c>TargetType</c> es "Panel" o "Inverter" y <c>FaultType</c> uno de
/// los nombres de <see cref="SimulatedFaultType"/>.
/// </summary>
public record InjectSimulatedFaultRequest(string TargetType, [property: JsonRequired] Guid TargetId, string FaultType);

/// <summary>Un equipo que emite lecturas, con su falla simulada vigente (la de mayor severidad), para la pantalla de fallas.</summary>
public record SimulationFaultTargetDto(
    Guid Id,
    string SerieNumber,
    string ParkName,
    string? NodeName,
    SimulatedFaultType? ActiveFault,
    string? ActiveFaultLabel,
    AlertSeverity? ActiveFaultSeverity);

/// <summary>Datos de la pantalla de simulación de fallas.</summary>
public record SimulationFaultOverviewDto(
    bool Enabled,
    IReadOnlyList<SimulationFaultTargetDto> Panels,
    IReadOnlyList<SimulationFaultTargetDto> Inverters);
