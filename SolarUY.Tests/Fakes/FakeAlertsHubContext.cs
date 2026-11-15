using Microsoft.AspNetCore.SignalR;
using SolarUY.ApiService.Hubs;

namespace SolarUY.Tests.Fakes;

/// <summary>SignalR falso: registra los mensajes que el servidor enviaría a cada grupo.</summary>
public class FakeAlertsHubContext : IHubContext<AlertsHub>
{
    private readonly RecordingClients _clients = new();

    public IHubClients Clients => _clients;

    public IGroupManager Groups { get; } = new NoOpGroupManager();

    /// <summary>Mensajes enviados: grupo, nombre del evento y primer argumento.</summary>
    public List<(string Group, string Method, object? Payload)> Sent => _clients.Sent;

    private sealed class RecordingClients : IHubClients
    {
        public List<(string Group, string Method, object? Payload)> Sent { get; } = [];

        public IClientProxy All => new Proxy(this, "*");
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new Proxy(this, "*");
        public IClientProxy Client(string connectionId) => new Proxy(this, connectionId);
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new Proxy(this, "*");
        public IClientProxy Group(string groupName) => new Proxy(this, groupName);
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new Proxy(this, groupName);
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => new Proxy(this, "*");
        public IClientProxy User(string userId) => new Proxy(this, userId);
        public IClientProxy Users(IReadOnlyList<string> userIds) => new Proxy(this, "*");
    }

    private sealed class Proxy(RecordingClients owner, string group) : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            owner.Sent.Add((group, method, args.Length > 0 ? args[0] : null));
            return Task.CompletedTask;
        }
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
