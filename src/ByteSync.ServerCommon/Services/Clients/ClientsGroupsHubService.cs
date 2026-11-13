using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services.Clients;

public class ClientsGroupsHubService : IClientsGroupsHubService
{
    private readonly IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> _hubContext;
    private readonly IClientsGroupIdFactory _clientsGroupIdFactory;
    private readonly ILogger<ClientsGroupsHubService> _logger;
    private readonly ByteSync.ServerCommon.Interfaces.Repositories.ICloudSessionsRepository _sessions;
    private readonly ByteSync.ServerCommon.Interfaces.Repositories.ILobbyRepository _lobbies;

    public ClientsGroupsHubService(IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> hubContext, IClientsGroupIdFactory clientsGroupIdFactory, 
        ILogger<ClientsGroupsHubService> logger, ByteSync.ServerCommon.Interfaces.Repositories.ICloudSessionsRepository sessions,
        ByteSync.ServerCommon.Interfaces.Repositories.ILobbyRepository lobbies)
    {
        _hubContext = hubContext;
        _clientsGroupIdFactory = clientsGroupIdFactory;
        _logger = logger;
        _sessions = sessions;
        _lobbies = lobbies;
    }
    
    public async Task AddToSessionGroup(Client client, string sessionId)
    {
        if (!await IsAuthorizedSubscription(client, _clientsGroupIdFactory.GetSessionGroupId(sessionId))) throw new UnauthorizedAccessException();
        await AddToGroup(client, _clientsGroupIdFactory.GetSessionGroupId(sessionId)).ConfigureAwait(false);
    }
    public async Task AddToLobbyGroup(Client client, string lobbyId)
    {
        if (!await IsAuthorizedSubscription(client, _clientsGroupIdFactory.GetLobbyGroupId(lobbyId))) throw new UnauthorizedAccessException();
        await AddToGroup(client, _clientsGroupIdFactory.GetLobbyGroupId(lobbyId)).ConfigureAwait(false);
    }
    
    private async Task AddToGroup(Client client, string groupName)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }

        foreach (var id in client.ConnectionIds.Distinct())
            await _hubContext.Groups.AddToGroupAsync(id, groupName).ConfigureAwait(false);
    }

    public async Task RemoveFromSessionGroup(Client client, string sessionId)
    {
        await RemoveFromGroup(client, _clientsGroupIdFactory.GetSessionGroupId(sessionId)).ConfigureAwait(false);
    }
    
    public async Task RemoveFromLobbyGroup(Client client, string lobbyId)
    {
        await RemoveFromGroup(client, _clientsGroupIdFactory.GetLobbyGroupId(lobbyId)).ConfigureAwait(false);
    }

    private async Task RemoveFromGroup(Client client, string groupName)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }
        
        foreach (var id in client.ConnectionIds.Distinct())
            await _hubContext.Groups.RemoveFromGroupAsync(id, groupName).ConfigureAwait(false);
    }

    public async Task AddClientGroup(string connectionId, Client client)
    {
        ByteSync.ServerCommon.Security.SessionAuthorizationBehavior<object, object>.RequireIdentity(client);
        await _hubContext.Groups.AddToGroupAsync(connectionId, _clientsGroupIdFactory.GetClientGroupId(client.ClientInstanceId)).ConfigureAwait(false);

        foreach (var subscribedGroup in client.SubscribedGroups)
        {
            if (!await IsAuthorizedSubscription(client, subscribedGroup)) continue;
            _logger.LogInformation("Adding client {clientInstanceId} to subscribed group {subscribedGroup}", client.ClientInstanceId, subscribedGroup);
            
            await _hubContext.Groups.AddToGroupAsync(connectionId, subscribedGroup).ConfigureAwait(false);
        }
    }

    private async Task<bool> IsAuthorizedSubscription(Client client, string group)
    {
        ByteSync.ServerCommon.Security.SessionAuthorizationBehavior<object, object>.RequireIdentity(client);
        var prefix = $"Auth{ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch}_";
        if (group.StartsWith(prefix + "Session_", StringComparison.Ordinal))
        {
            var session = await _sessions.Get(group[(prefix.Length + "Session_".Length)..]);
            return session is { IsSessionRemoved: false } && session.AuthenticationEpoch == ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch && session.FindMember(client.ClientInstanceId)?.Identity == client.Identity;
        }
        if (group.StartsWith(prefix + "Lobby_", StringComparison.Ordinal))
        {
            var lobby = await _lobbies.Get(group[(prefix.Length + "Lobby_".Length)..]);
            return lobby?.AuthenticationEpoch == ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch && lobby.GetLobbyMemberByClientInstanceId(client.ClientInstanceId) != null;
        }
        return false;
    }

    public async Task RemoveClientGroup(string connectionId, Client client)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, _clientsGroupIdFactory.GetClientGroupId(client.ClientInstanceId)).ConfigureAwait(false);
    }
}