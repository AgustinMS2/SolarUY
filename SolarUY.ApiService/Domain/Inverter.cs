namespace SolarUY.ApiService.Domain;

public class Inverter
{
    public Guid Id { get; set; }
    public string SerieNumber { get; set; } = string.Empty;
    public string NormalizedSerieNumber { get; set; } = string.Empty;
    public string BrandAndModel { get; set; } = string.Empty;
    public double NominalCapacityKw { get; set; }
    public string? Endpoint { get; set; }
    public Guid SolarNodeId { get; set; }
    public SolarNode? SolarNode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
