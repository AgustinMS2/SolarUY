namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Telemetría eléctrica y térmica en tiempo real de un nodo solar y su inversor (Spec 028).
/// </summary>
public class NodeTelemetryDto
{
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public string InverterSerieNumber { get; set; } = string.Empty;
    public double NominalCapacityKw { get; set; }
    public double ActivePowerKw { get; set; }
    public double DcVoltage { get; set; }
    public double DcCurrent { get; set; }
    public double AcVoltage { get; set; }
    public double GridFrequencyHz { get; set; }
    public double InverterTemperatureC { get; set; }
    public bool IsOverheating { get; set; }
    public bool IsGridFrequencyAnomaly { get; set; }
    public DateTime TimestampUtc { get; set; }
}
