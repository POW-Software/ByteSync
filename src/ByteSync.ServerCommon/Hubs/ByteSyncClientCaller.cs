using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Hubs;

public class ByteSyncClientCaller : IByteSyncClientCaller
{
    private readonly IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> _hubContext;
    private readonly ILogger<ByteSyncClientCaller> _logger;

    private const string SESSION_PREFIX = "Session_";
    private const string LOBBY_PREFIX = "Lobby_";
    private const string CLIENT_PREFIX = "CGID_";

    public ByteSyncClientCaller(IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> hubContext, ILogger<ByteSyncClientCaller> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public IHubByteSyncPush Client(string clientInstanceId)
    {
        var groupId = GetClientGroupId(clientInstanceId);
        
        return _hubContext.Clients.Group(groupId);
    }

    public IHubByteSyncPush Client(Client client)
    {
        return Client(client.ClientInstanceId);
    }

    public IHubByteSyncPush Client(SessionMemberData sessionMemberData)
    {
        return Client(sessionMemberData.ClientInstanceId);
    }

    public IHubByteSyncPush Clients(ICollection<string> clientInstanceIds)
    {
        var groupIds = new List<string>();
        foreach (var clientInstanceId in clientInstanceIds)
        {
            groupIds.Add(GetClientGroupId(clientInstanceId));
        }
        
        return _hubContext.Clients.Groups(groupIds);
    }

    public IHubByteSyncPush Clients(ICollection<SessionMemberData> sessionMemberDatas)
    {
        return Clients(sessionMemberDatas.Select(sm => sm.ClientInstanceId).ToList());
    }

    public IHubByteSyncPush SessionGroup(string sessionId)
    {
        return _hubContext.Clients.Group(GetSessionGroupId(sessionId));
    }
    
    public IHubByteSyncPush LobbyGroup(string lobbyId)
    {
        return _hubContext.Clients.Group(GetLobbyGroupId(lobbyId));
    }
    
    public IHubByteSyncPush SessionGroupExcept(string sessionId, Client client)
    {
        return GroupExcept(GetSessionGroupId(sessionId), client);
    }
    
    public IHubByteSyncPush LobbyGroupExcept(string lobbyId, Client client)
    {
        return GroupExcept(GetLobbyGroupId(lobbyId), client);
    }

    private IHubByteSyncPush GroupExcept(string groupId, Client client)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        if (connectionId != null)
        {
            return _hubContext.Clients.GroupExcept(groupId, client.ConnectionIds.Last());
        }
        else
        {
            return _hubContext.Clients.Group(groupId);
        }
    }

    public async Task AddToSessionGroup(Client client, string sessionId)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }
        
        await _hubContext.Groups.AddToGroupAsync(connectionId, GetSessionGroupId(sessionId));
    }
    
    public async Task AddToLobbyGroup(Client client, string lobbyId)
    {
        var connectionId = client.ConnectionIds.LastOrDefault();
        
        if (connectionId == null)
        {
            _logger.LogWarning("ConnectionId is null for client {clientInstanceId}", client.ClientInstanceId);
            return;
        }
        
        await _hubContext.Groups.AddToGroupAsync(connectionId, GetLobbyGroupId(lobbyId));
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
        await _hubContext.Groups.AddToGroupAsync(connectionId, GetClientGroupId(client.ClientInstanceId));
    }

    public async Task RemoveClientGroup(string connectionId, Client client)
    {
        await _hubContext.Groups.RemoveFromGroupAsync(connectionId, GetClientGroupId(client.ClientInstanceId));
    }

    private string GetClientGroupId(string clientInstanceId)
    {
        return CLIENT_PREFIX + clientInstanceId;
    }
    
    private string GetSessionGroupId(string sessionId)
    {
        return SESSION_PREFIX + sessionId;
    }
    
    private string GetLobbyGroupId(string lobbyId)
    {
        return LOBBY_PREFIX + lobbyId;
    }
}