namespace SolarUY.Web.Components;

/// <summary>A qué se aplica la orden de inyección a la red (spec 033).</summary>
public enum GridInjectionScope
{
    /// <summary>Todos los inversores del parque.</summary>
    Park,

    /// <summary>El inversor de un nodo.</summary>
    Inverter
}

/// <summary>Resultado de enviar una orden de corte o reanudación de la inyección a la red.</summary>
public record GridInjectionSubmitResult(bool Success, string? Message);
