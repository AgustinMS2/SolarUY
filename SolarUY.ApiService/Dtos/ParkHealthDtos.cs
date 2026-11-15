namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Lectura de salud de un panel en un instante: estado, potencia y eficiencia (Spec 030).
/// </summary>
public class PanelHealthReadingDto
{
    public Guid PanelId { get; set; }
    public PanelHealthState State { get; set; }

    /// <summary>Potencia actual en W; nula cuando el panel no recibe lecturas.</summary>
    public double? ActivePowerW { get; set; }

    public double ExpectedPowerW { get; set; }

    /// <summary>Eficiencia estimada en vivo; nula sin sol o sin lecturas.</summary>
    public double? EstimatedEfficiencyPercent { get; set; }

    /// <summary>Generación inferior al 40 % de lo esperado (misma regla que la spec 029).</summary>
    public bool IsLowPerformance { get; set; }

    /// <summary>Motivo cuando el panel no recibe lecturas (baja, sin nodo o nodo inactivo).</summary>
    public string? StateReason { get; set; }
}

/// <summary>Contadores de la cabecera del mapa de salud (RF-4).</summary>
public class ParkHealthSummaryDto
{
    public int Total { get; set; }
    public int Normal { get; set; }
    public int Degraded { get; set; }
    public int Critical { get; set; }
    public int NoSun { get; set; }
    public int Inactive { get; set; }
}

/// <summary>Evento que viaja por SignalR: un solo mensaje por parque y ciclo (RF-5).</summary>
public class ParkHealthUpdateDto
{
    public Guid ParkId { get; set; }
    public DateTime TimestampUtc { get; set; }
    public ParkHealthSummaryDto Summary { get; set; } = new();
    public List<PanelHealthReadingDto> Panels { get; set; } = new();
}

/// <summary>Celda del mapa: datos fijos del panel más su primera lectura.</summary>
public class ParkHealthPanelDto
{
    public Guid PanelId { get; set; }
    public string SerieNumber { get; set; } = string.Empty;
    public int NominalPowerW { get; set; }
    public double NominalEfficiencyPercent { get; set; }
    public DateTime? LastMaintenanceUtc { get; set; }
    public PanelHealthReadingDto Reading { get; set; } = new();
}

/// <summary>Grupo de paneles de un nodo; <see cref="NodeId"/> es nulo para "Sin nodo asignado".</summary>
public class ParkHealthNodeDto
{
    public Guid? NodeId { get; set; }
    public string NodeName { get; set; } = string.Empty;
    public List<ParkHealthPanelDto> Panels { get; set; } = new();
}

/// <summary>Carga inicial de la pantalla del mapa de salud.</summary>
public class ParkHealthSnapshotDto
{
    public Guid ParkId { get; set; }
    public string ParkName { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public ParkHealthSummaryDto Summary { get; set; } = new();
    public List<ParkHealthNodeDto> Nodes { get; set; } = new();
}
