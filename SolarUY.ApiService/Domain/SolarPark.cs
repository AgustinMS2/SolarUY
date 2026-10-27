namespace SolarUY.ApiService.Domain;

public class SolarPark
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double MaxCapacityKw { get; set; }
    public double PowerAlertThresholdKw { get; set; }
    public SolarParkStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
