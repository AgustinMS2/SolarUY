namespace SolarUY.ApiService.Services;

/// <summary>
/// Envío de órdenes de control hacia la API del inversor/simulador (spec 032).
/// Devuelve false cuando la orden no pudo confirmarse (pérdida de comunicación).
/// La implementación real contra Inverter.Endpoint llega con la spec 036.
/// </summary>
public interface IInverterCommandSender
{
    bool SendDeactivate(string panelSerieNumber, string? inverterEndpoint);

    bool SendActivate(string panelSerieNumber, string? inverterEndpoint);
}
