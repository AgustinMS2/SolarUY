namespace SolarUY.ApiService.Domain;

/// <summary>
/// Resumen de un minuto de funcionamiento de un inversor (Spec 036, RF-3). Es la historia detallada del parque:
/// guarda más variables que las que hoy consultan los reportes, para cubrir futuras necesidades de información.
/// La clave es (inversor, minuto): solo puede existir un registro por inversor y minuto.
/// </summary>
/// <remarks>
/// Es una tabla de historia de solo agregado: no tiene llaves foráneas a propósito, para que dar de baja o
/// desvincular un inversor, un nodo o un parque no borre ni bloquee su historia.
/// </remarks>
public class InverterMinuteReading
{
    public Guid InverterId { get; set; }

    /// <summary>Inicio del minuto, en UTC.</summary>
    public DateTime MinuteUtc { get; set; }

    /// <summary>Parque al que pertenecía el inversor en ese momento.</summary>
    public Guid SolarParkId { get; set; }

    /// <summary>Nodo al que pertenecía el inversor en ese momento.</summary>
    public Guid SolarNodeId { get; set; }

    public double AvgPowerKw { get; set; }

    public double MaxPowerKw { get; set; }

    /// <summary>Energía generada en el minuto, en kWh.</summary>
    public double EnergyKwh { get; set; }

    public double AvgDcVoltageV { get; set; }

    public double AvgDcCurrentA { get; set; }

    public double AvgAcVoltageV { get; set; }

    public double AvgFrequencyHz { get; set; }

    public double AvgTemperatureC { get; set; }

    public double MaxTemperatureC { get; set; }

    /// <summary>Cantidad de lecturas del minuto (6, una cada 10 s).</summary>
    public int SampleCount { get; set; }

    /// <summary>Lecturas del minuto con frecuencia fuera de 50 ± 0,5 Hz o temperatura sobre 75 °C.</summary>
    public int AnomalousSamples { get; set; }
}
