using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Hubs;

[Authorize(Roles = Roles.Any)]
public class ParkTelemetryHub : Hub
{
    public const string HubUrl = "/hubs/park-telemetry";
    public const string ReceiveEventName = "ReceiveParkTelemetry";

    public static string GetGroupName(Guid parkId) => $"park-{parkId}";

    public async Task SubscribeToPark(Guid parkId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(parkId));
    }

    public async Task UnsubscribeFromPark(Guid parkId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(parkId));
    }
}

