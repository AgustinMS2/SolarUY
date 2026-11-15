namespace SolarUY.ApiService.Domain;

// Spec 032: bitácora de auditoría de órdenes de control sobre paneles (quién, cuándo, panel y orden enviada).
public class PanelControlEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SolarPanelId { get; set; }

    public SolarPanel? SolarPanel { get; set; }

    public PanelControlAction Action { get; set; }

    // Usuario autenticado que ordenó la acción (RF-2: registro de auditoría estricto).
    public string OrderedBy { get; set; } = string.Empty;

    public DateTime OrderedAtUtc { get; set; }

    // false cuando el panel no tiene nodo o el nodo no tiene inversor (no hay orden que enviar).
    public bool OrderSentToInverter { get; set; }

    public string? InverterSerieNumber { get; set; }
}

public enum PanelControlAction
{
    /// <summary>Orden de corte eléctrico (RF-2).</summary>
    Deactivate = 1,

    /// <summary>Orden de reconexión / reanudación de la inyección (RF-4).</summary>
    Activate = 2
}
