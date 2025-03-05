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
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }
        
        await _hubContext.Groups.AddToGroupAsync(connectionId, _clientsGroupIdFactory.GetSessionGroupId(sessionId));
    }
    
    public async Task AddToLobbyGroup(Client client, string lobbyId)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }
        
        await _hubContext.Groups.AddToGroupAsync(connectionId, _clientsGroupIdFactory.GetLobbyGroupId(lobbyId));
    }
    
    public async Task RemoveFromGroup(Client client, string groupName)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }
        
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName);
    }

    public async Task AddClientGroup(string connectionId, Client client)
    {
        await _hubContext.Groups.AddToGroupAsync(connectionId, _clientsGroupIdFactory.GetClientGroupId(client.ClientInstanceId));
    }

    public async Task RemoveClientGroup(string connectionId, Client client)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, _clientsGroupIdFactory.GetClientGroupId(client.ClientInstanceId));
    }
}