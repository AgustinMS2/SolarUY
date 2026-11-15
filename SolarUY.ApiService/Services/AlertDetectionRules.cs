using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Reglas puras que convierten una lectura de telemetría en una condición de falla (Spec 031, RF-1).
/// Hoy existen las condiciones con datos disponibles y las fallas simuladas de la spec 037 (aislamiento del inversor y
/// degradación de un panel); los informativos de la spec 033 se suman cuando existan (criterio provisorio).
/// </summary>
public static class AlertDetectionRules
{
    /// <summary>Advertencia: generación del parque por debajo de su umbral con sol (spec 027).</summary>
    public static FaultDetection? FromParkTelemetry(ParkTelemetryDto telemetry)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (!telemetry.IsAlertActive)
        {
            return null;
        }

        return new FaultDetection(
            telemetry.ParkId,
            telemetry.ParkName,
            AlertEquipmentType.Park,
            telemetry.ParkId,
            telemetry.ParkName,
            AlertSeverity.Warning,
            $"Generación por debajo del umbral de alerta ({telemetry.AlertThresholdKw:0.##} kW): {telemetry.CurrentPowerKw:0.0} kW actuales.");
    }

    /// <summary>
    /// Crítico: falla de aislamiento del inversor, apagado por la protección de fuga a tierra (spec 037). Advertencia:
    /// temperatura interna del inversor por encima de 75 °C (spec 028). La falla de aislamiento prevalece.
    /// </summary>
    public static FaultDetection? FromNodeTelemetry(NodeTelemetryDto telemetry, Guid parkId, string parkName)
    {
        ArgumentNullException.ThrowIfNull(telemetry);

        if (telemetry.Fault == SimulatedFaultType.InsulationFault)
        {
            return new FaultDetection(
                parkId,
                parkName,
                AlertEquipmentType.Node,
                telemetry.NodeId,
                telemetry.NodeName,
                AlertSeverity.Critical,
                "Falla de aislamiento en inversor: protección de fuga a tierra, inversor apagado.");
        }

        if (!telemetry.IsOverheating)
        {
            return null;
        }

        return new FaultDetection(
            parkId,
            parkName,
            AlertEquipmentType.Node,
            telemetry.NodeId,
            telemetry.NodeName,
            AlertSeverity.Warning,
            $"Temperatura alta en inversor ({telemetry.InverterTemperatureC:0.0} °C).");
    }

    /// <summary>
    /// Crítico: panel en Falla Crítica, con menos del 10 % de lo esperado con sol (spec 030). Advertencia: panel con la
    /// falla simulada de degradación severa (spec 037); la baja generación ocasional sin falla no genera alerta.
    /// </summary>
    public static FaultDetection? FromPanelHealth(PanelHealthReadingDto reading, Guid parkId, string parkName, string panelSerieNumber)
    {
        ArgumentNullException.ThrowIfNull(reading);

        if (reading.State == PanelHealthState.Critical)
        {
            var cause = reading.Fault.HasValue ? $" Falla simulada: {FaultCatalog.LabelOf(reading.Fault.Value)}." : string.Empty;
            return new FaultDetection(
                parkId,
                parkName,
                AlertEquipmentType.Panel,
                reading.PanelId,
                panelSerieNumber,
                AlertSeverity.Critical,
                $"Panel sin generación con sol: {reading.ActivePowerW ?? 0.0:0.0} W de {reading.ExpectedPowerW:0.0} W esperados.{cause}");
        }

        if (reading.State == PanelHealthState.Degraded && reading.Fault == SimulatedFaultType.SevereDegradation)
        {
            return new FaultDetection(
                parkId,
                parkName,
                AlertEquipmentType.Panel,
                reading.PanelId,
                panelSerieNumber,
                AlertSeverity.Warning,
                $"Degradación severa del panel: {reading.ActivePowerW ?? 0.0:0.0} W de {reading.ExpectedPowerW:0.0} W esperados.");
        }

        return null;
    }
}
