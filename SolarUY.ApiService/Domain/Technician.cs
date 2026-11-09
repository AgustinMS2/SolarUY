namespace SolarUY.ApiService.Domain;

public class Technician
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string DocumentId { get; set; } = string.Empty;

    public string NormalizedDocumentId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Specialty { get; set; }

    public TechnicianStatus Status { get; set; } = TechnicianStatus.Active;

    public DateTime CreatedAtUtc { get; set; }
}

public enum TechnicianStatus
{
    Active = 1,
    Inactive = 2
}