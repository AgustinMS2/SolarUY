using SolarUY.ApiService.Domain;
using SolarUY.ApiService.Dtos;
using SolarUY.ApiService.Simulation;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Reglas de la Spec 030. El estado se calcula solo a partir de la generación contra lo esperado a
/// esa hora (criterio provisorio; los eventos de falla de las specs 031 y 037 se suman más adelante).
/// </summary>
public class PanelHealthService : IPanelHealthService
{
    private const double NormalRatio = 0.80;
    private const double CriticalRatio = 0.10;
    private const double LowPerformanceRatio = 0.40;

    private readonly IPanelTelemetryService _telemetryService;

    public PanelHealthService(IPanelTelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    public PanelHealthReadingDto ClassifyHealth(SolarPanel panel, bool hasActiveNode, double activePowerW, DateTime localTime, SimulatedFaultType? fault = null)
    {
        ArgumentNullException.ThrowIfNull(panel);

        // Sin lecturas: panel dado de baja, sin nodo o con nodo inactivo (la spec 029 no emite para ellos).
        var noReadingsReason = GetNoReadingsReason(panel, hasActiveNode);
        if (noReadingsReason is not null)
        {
            return new PanelHealthReadingDto
            {
                PanelId = panel.Id,
                State = PanelHealthState.Inactive,
                ActivePowerW = null,
                ExpectedPowerW = 0.0,
                EstimatedEfficiencyPercent = null,
                IsLowPerformance = false,
                StateReason = noReadingsReason
            };
        }

        var activePower = Math.Max(0.0, activePowerW);
        var expectedPower = _telemetryService.CalculateExpectedPowerW(panel.PowerW, localTime);

        // Falla simulada vigente (spec 037): se informa como motivo y como tipo, también de noche, cuando todavía no se nota.
        var faultLabel = fault.HasValue ? FaultCatalog.LabelOf(fault.Value) : null;
        var faultReason = faultLabel is null ? null : $"Falla simulada: {faultLabel}";

        // De noche no hay referencia para evaluar el rendimiento.
        if (expectedPower <= 0.0)
        {
            return new PanelHealthReadingDto
            {
                PanelId = panel.Id,
                State = PanelHealthState.NoSun,
                ActivePowerW = activePower,
                ExpectedPowerW = 0.0,
                EstimatedEfficiencyPercent = null,
                IsLowPerformance = false,
                StateReason = faultReason,
                Fault = fault,
                FaultLabel = faultLabel
            };
        }

        var ratio = activePower / expectedPower;

        var state = ratio >= NormalRatio
            ? PanelHealthState.Normal
            : ratio < CriticalRatio
                ? PanelHealthState.Critical
                : PanelHealthState.Degraded;

        return new PanelHealthReadingDto
        {
            PanelId = panel.Id,
            State = state,
            ActivePowerW = activePower,
            ExpectedPowerW = expectedPower,
            EstimatedEfficiencyPercent = Math.Round(panel.Efficiency * ratio, 1),
            IsLowPerformance = ratio < LowPerformanceRatio,
            StateReason = faultReason,
            Fault = fault,
            FaultLabel = faultLabel
        };
    }

    public ParkHealthSummaryDto BuildSummary(IEnumerable<PanelHealthState> states)
    {
        ArgumentNullException.ThrowIfNull(states);

        var summary = new ParkHealthSummaryDto();
        foreach (var state in states)
        {
            summary.Total++;
            switch (state)
            {
                case PanelHealthState.Normal: summary.Normal++; break;
                case PanelHealthState.Degraded: summary.Degraded++; break;
                case PanelHealthState.Critical: summary.Critical++; break;
                case PanelHealthState.NoSun: summary.NoSun++; break;
                case PanelHealthState.Inactive: summary.Inactive++; break;
            }
        }

        return summary;
    }

    public IReadOnlyDictionary<Guid, DateTime> LatestCompletedByPanel(IEnumerable<MaintenanceOrder> orders)
    {
        ArgumentNullException.ThrowIfNull(orders);

        return orders
            .Where(o => o.Status == MaintenanceStatus.Completed && o.CompletedAtUtc.HasValue)
            .GroupBy(o => o.SolarPanelId)
            .ToDictionary(g => g.Key, g => g.Max(o => o.CompletedAtUtc!.Value));
    }

    private static string? GetNoReadingsReason(SolarPanel panel, bool hasActiveNode)
    {
        if (panel.Status == SolarPanelStatus.Deactivated)
        {
            return "Panel desactivado manualmente";
        }

        if (panel.Status != SolarPanelStatus.Active)
        {
            return "Panel dado de baja";
        }

        if (panel.SolarNodeId is null)
        {
            return "Sin nodo asignado";
        }

        return hasActiveNode ? null : "Nodo inactivo";
    }
}
