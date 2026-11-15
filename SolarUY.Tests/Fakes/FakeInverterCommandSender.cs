using SolarUY.ApiService.Services;

namespace SolarUY.Tests.Fakes;

/// <summary>
/// Fake de IInverterCommandSender (spec 032): permite simular el envío exitoso
/// de la orden al inversor y la pérdida de comunicación (Succeeds = false).
/// </summary>
public class FakeInverterCommandSender : IInverterCommandSender
{
    /// <summary>Resultado del envío para todos los comandos (true = confirmado).</summary>
    public bool Succeeds { get; set; } = true;

    /// <summary>Números de serie de los paneles con orden de corte enviada.</summary>
    public List<string> SentDeactivateCommands { get; } = [];

    /// <summary>Números de serie de los paneles con orden de habilitación enviada.</summary>
    public List<string> SentActivateCommands { get; } = [];

    public bool SendDeactivate(string panelSerieNumber, string? inverterEndpoint)
    {
        SentDeactivateCommands.Add(panelSerieNumber);
        return Succeeds;
    }

    public bool SendActivate(string panelSerieNumber, string? inverterEndpoint)
    {
        SentActivateCommands.Add(panelSerieNumber);
        return Succeeds;
    }
}
