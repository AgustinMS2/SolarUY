using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Hubs;

/// <summary>
/// Hub de telemetría en tiempo real de nodos solares e inversores (Spec 028, RF-1 y RF-7).
/// Cada conexión se suscribe al grupo del nodo correspondiente para recibir lecturas periódicas.
/// </summary>
[Authorize(Roles = Roles.Any)]
public class NodeTelemetryHub : Hub
{
    public const string HubUrl = "/hubs/node-telemetry";
    public const string ReceiveEventName = "ReceiveNodeTelemetry";

    public static string GetGroupName(Guid nodeId) => $"node-{nodeId}";

    public async Task SubscribeToNode(Guid nodeId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(nodeId));
    }

    public async Task UnsubscribeFromNode(Guid nodeId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(nodeId));
    }
}
