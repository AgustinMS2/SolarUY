namespace SolarUY.ApiService.Dtos;

/// <summary>
/// Estado de salud de un panel en el mapa de salud de un parque (Spec 030).
/// </summary>
public enum PanelHealthState
{
    /// <summary>Verde: genera al menos el 80 % de lo esperado a esa hora.</summary>
    Normal = 1,

    /// <summary>Ámbar: genera entre el 10 % (inclusive) y el 80 % (exclusivo) de lo esperado.</summary>
    Degraded = 2,

    /// <summary>Rojo: genera menos del 10 % de lo esperado con sol.</summary>
    Critical = 3,

    /// <summary>Índigo: potencia esperada de 0 W (de noche); no se evalúa el rendimiento.</summary>
    NoSun = 4,

    /// <summary>Gris: panel dado de baja, sin nodo o con nodo inactivo (no recibe lecturas).</summary>
    Inactive = 5
}
