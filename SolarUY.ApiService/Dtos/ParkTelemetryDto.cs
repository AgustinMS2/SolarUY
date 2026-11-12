namespace SolarUY.ApiService.Dtos;

/// <summary>
/// MÃ©tricas de telemetrÃ­a en tiempo real transmitidas para un parque solar (Spec 027).
/// </summary>
public class ParkTelemetryDto
{
    public Guid ParkId { get; set; }

    public string ParkName { get; set; } = string.Empty;

    public double CurrentPowerKw { get; set; }

    public double MaxCapacityKw { get; set; }

    public double CapacityPercentage { get; set; }

    public double EnergyTodayKwh { get; set; }

    public double AlertThresholdKw { get; set; }

    public bool IsAlertActive { get; set; }

    public DateTime TimestampUtc { get; set; }
}

