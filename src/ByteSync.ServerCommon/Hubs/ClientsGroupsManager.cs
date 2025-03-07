using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Interfaces.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Hubs;

public class ClientsGroupsManager : IClientsGroupsManager
{
    private readonly IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> _hubContext;
    private readonly IClientsGroupIdFactory _clientsGroupIdFactory;
    private readonly ILogger<ClientsGroupsManager> _logger;

    public ClientsGroupsManager(IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> hubContext, IClientsGroupIdFactory clientsGroupIdFactory, 
        ILogger<ClientsGroupsManager> logger)
    {
        _hubContext = hubContext;
        _clientsGroupIdFactory = clientsGroupIdFactory;
        _logger = logger;
    }
    
    public async Task AddToSessionGroup(Client client, string sessionId)
    {
        await AddToGroup(client, _clientsGroupIdFactory.GetSessionGroupId(sessionId)).ConfigureAwait(false);
    }
    public async Task AddToLobbyGroup(Client client, string lobbyId)
    {
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

        await _hubContext.Groups.AddToGroupAsync(connectionId, groupName).ConfigureAwait(false);
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
        
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName).ConfigureAwait(false);
    }

    public async Task AddClientGroup(string connectionId, Client client)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, _clientsGroupIdFactory.GetClientGroupId(client.ClientInstanceId)).ConfigureAwait(false);
    }

    public async Task RemoveClientGroup(string connectionId, Client client)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, _clientsGroupIdFactory.GetClientGroupId(client.ClientInstanceId)).ConfigureAwait(false);
    }
}