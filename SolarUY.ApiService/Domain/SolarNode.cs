namespace SolarUY.ApiService.Domain;

public class SolarNode
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public Guid SolarParkId { get; set; }
    public SolarPark? SolarPark { get; set; }
    public SolarNodeStatus Status { get; set; } = SolarNodeStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public enum SolarNodeStatus
{
    Active = 1,
    Inactive = 2
}