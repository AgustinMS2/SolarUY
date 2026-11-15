using Microsoft.Extensions.Logging;

namespace SolarUY.ApiService.Services;

/// <summary>
/// Spec 032: stub provisorio del envío de órdenes al inversor/simulador.
/// Acepta la orden y la registra en el log para poder verificar el flujo completo
/// hasta que exista el simulador real (spec 036), que aportará el cliente HTTP.
/// </summary>
public class InMemoryInverterCommandSender : IInverterCommandSender
{
    private readonly ILogger<InMemoryInverterCommandSender> _logger;

    public InMemoryInverterCommandSender(ILogger<InMemoryInverterCommandSender> logger)
    {
        _logger = logger;
    }

    public bool SendDeactivate(string panelSerieNumber, string? inverterEndpoint)
    {
        _logger.LogInformation(
            "Orden de corte eléctrico aceptada para el panel {PanelSerie} (inversor: {Endpoint})",
            panelSerieNumber, inverterEndpoint ?? "sin inversor asociado");
        return true;
    }

    public bool SendActivate(string panelSerieNumber, string? inverterEndpoint)
    {
        _logger.LogInformation(
            "Orden de habilitación aceptada para el panel {PanelSerie} (inversor: {Endpoint})",
            panelSerieNumber, inverterEndpoint ?? "sin inversor asociado");
        return true;
    }

    // Spec 033: stub de las órdenes de inyección a la red hasta la spec 036.
    public bool SendInjectionCut(string inverterSerieNumber, string? inverterEndpoint)
    {
        _logger.LogInformation(
            "Orden de corte de inyección a la red aceptada para el inversor {InverterSerie} (endpoint: {Endpoint})",
            inverterSerieNumber, inverterEndpoint ?? "sin endpoint");
        return true;
    }

    public bool SendInjectionResume(string inverterSerieNumber, string? inverterEndpoint)
    {
        _logger.LogInformation(
            "Orden de reanudación de inyección a la red aceptada para el inversor {InverterSerie} (endpoint: {Endpoint})",
            inverterSerieNumber, inverterEndpoint ?? "sin endpoint");
        return true;
    }
}
