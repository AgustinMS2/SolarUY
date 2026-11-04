namespace SolarUY.ApiService.Domain;

public class SolarPanel
{
    public Guid Id { get; set; }
    public string SerieNumber { get; set; } = string.Empty;
    public string NormalizedSerieNumber { get; set; } = string.Empty;
    public int PowerW { get; set; }
    public double Efficiency { get; set; }
    public Guid SolarParkId { get; set; }
    public SolarPark? SolarPark { get; set; }
    public Guid? SolarNodeId { get; set; }
    public SolarNode? SolarNode { get; set; }
    public SolarPanelStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
