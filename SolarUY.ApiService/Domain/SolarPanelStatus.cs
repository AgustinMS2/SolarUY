namespace SolarUY.ApiService.Domain;

public enum SolarPanelStatus
{
    Active = 1,
    Inactive = 2,

    /// <summary>
    /// Spec 032: desactivación eléctrica remota; distinto de Inactive (baja lógica, spec 008).
    /// </summary>
    Deactivated = 3
}
