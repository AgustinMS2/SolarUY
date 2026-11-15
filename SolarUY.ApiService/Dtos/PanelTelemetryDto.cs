namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Telemetría en tiempo real de un panel solar: lectura instantánea, comparación contra
/// la potencia de placa y contra la potencia esperada a la hora actual (Spec 029).
/// </summary>
public class PanelTelemetryDto
{
    public Guid PanelId { get; set; }
    public string SerieNumber { get; set; } = string.Empty;
    public string ParkName { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public int NominalPowerW { get; set; }
    public double NominalEfficiencyPercent { get; set; }
    public double ActivePowerW { get; set; }
    public double ExpectedPowerW { get; set; }
    public double PercentOfNominal { get; set; }

    /// <summary>Eficiencia estimada en vivo; nula cuando la potencia esperada es 0 W (de noche).</summary>
    public double? EstimatedEfficiencyPercent { get; set; }

    public bool IsLowPerformance { get; set; }
    public DateTime TimestampUtc { get; set; }
}
