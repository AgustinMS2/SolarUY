namespace SolarUY.ApiService.Simulation;

/// <summary>Configuración de la simulación (spec 036, RF-1): sección <c>Simulation</c> de <c>appsettings</c>.</summary>
public class SimulationOptions
{
    public const string SectionName = "Simulation";

    /// <summary>Si es falso, el servidor no guarda historia ni emite lecturas simuladas.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Segundos entre dos emisiones de lecturas en vivo (specs 027 a 030).</summary>
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>Días de historia por minuto que se generan al arrancar si no hay historia previa.</summary>
    public int BackfillDays { get; set; } = 30;
}
