using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Hubs;

/// <summary>
/// Hub de telemetría en tiempo real de paneles solares (Spec 029, RF-1 y RF-6).
/// Cada conexión se suscribe al grupo del panel correspondiente para recibir lecturas periódicas.
/// </summary>
[Authorize(Roles = Roles.Any)]
public class PanelTelemetryHub : Hub
{
    public const string HubUrl = "/hubs/panel-telemetry";
    public const string ReceiveEventName = "ReceivePanelTelemetry";

    public static string GetGroupName(Guid panelId) => $"panel-{panelId}";

    public async Task SubscribeToPanel(Guid panelId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(panelId));
    }

    public async Task UnsubscribeFromPanel(Guid panelId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(panelId));
    }
}
