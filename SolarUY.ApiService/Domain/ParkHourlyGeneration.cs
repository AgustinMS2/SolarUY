namespace SolarUY.ApiService.Domain;

/// <summary>
/// Registro horario de energía generada y potencia pico por parque solar (Spec 034).
/// </summary>
public class ParkHourlyGeneration
{
    public Guid Id { get; set; }

    public Guid SolarParkId { get; set; }

    public SolarPark? SolarPark { get; set; }

    /// <summary>
    /// Marca temporal exacta del inicio de la hora en UTC.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Fecha del registro en hora local de Uruguay (America/Montevideo).
    /// </summary>
    public DateOnly DateLocal { get; set; }

    /// <summary>
    /// Hora del día local (0 a 23) en hora de Uruguay.
    /// </summary>
    public int HourLocal { get; set; }

    /// <summary>
    /// Energía acumulada generada en la hora, expresada en kWh.
    /// </summary>
    public double EnergyGeneratedKwh { get; set; }

    /// <summary>
    /// Máxima potencia instantánea alcanzada en dicha hora, expresada en kW.
    /// </summary>
    public double PeakPowerKw { get; set; }
}

