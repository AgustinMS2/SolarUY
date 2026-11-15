using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Hubs;

/// <summary>
/// Hub del mapa de salud de paneles de un parque (Spec 030, RF-5 y RF-7).
/// Cada conexión se suscribe al grupo del parque para recibir un evento por ciclo con el estado de todos sus paneles.
/// </summary>
[Authorize(Roles = Roles.Any)]
public class ParkHealthHub : Hub
{
    public const string HubUrl = "/hubs/park-health";
    public const string ReceiveEventName = "ReceiveParkHealth";

    public static string GetGroupName(Guid parkId) => $"park-health-{parkId}";

    public async Task SubscribeToPark(Guid parkId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(parkId));
    }

    public async Task UnsubscribeFromPark(Guid parkId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(parkId));
    }
}
