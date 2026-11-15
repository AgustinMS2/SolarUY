using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SolarUY.ApiService.Security;

namespace SolarUY.ApiService.Hubs;

/// <summary>
/// Hub de alertas de falla (Spec 031). Todo usuario autenticado entra al grupo de alertas al conectarse y
/// recibe cada alerta nueva o consolidada y cada cambio de estado, para actualizar la campana y los avisos.
/// </summary>
[Authorize(Roles = Roles.Any)]
public class AlertsHub : Hub
{
    public const string HubUrl = "/hubs/alerts";
    public const string ReceiveEventName = "ReceiveAlertEvent";
    public const string GroupName = "alerts";

    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName);
        await base.OnConnectedAsync();
    }
}
