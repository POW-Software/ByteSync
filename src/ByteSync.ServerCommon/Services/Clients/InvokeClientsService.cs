using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.AspNetCore.SignalR;

namespace ByteSync.ServerCommon.Services.Clients;

public class InvokeClientsService : IInvokeClientsService
{
    private readonly IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> _hubContext;
    private readonly IClientsGroupIdFactory _clientsGroupIdFactory;

    public InvokeClientsService(IHubContext<Hub<IHubByteSyncPush>, IHubByteSyncPush> hubContext, IClientsGroupIdFactory clientsGroupIdFactory)
    {
        _hubContext = hubContext;
        _clientsGroupIdFactory = clientsGroupIdFactory;
    }

    public IHubByteSyncPush Client(string clientInstanceId)
    {
        var groupId = _clientsGroupIdFactory.GetClientGroupId(clientInstanceId);
        
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
            groupIds.Add(_clientsGroupIdFactory.GetClientGroupId(clientInstanceId));
        }
        
        return _hubContext.Clients.Groups(groupIds);
    }

    public IHubByteSyncPush Clients(ICollection<SessionMemberData> sessionMemberDatas)
    {
        return Clients(sessionMemberDatas.Select(sm => sm.ClientInstanceId).ToList());
    }

    public IHubByteSyncPush SessionGroup(string sessionId)
    {
        return _hubContext.Clients.Group(_clientsGroupIdFactory.GetSessionGroupId(sessionId));
    }
    
    public IHubByteSyncPush LobbyGroup(string lobbyId)
    {
        return _hubContext.Clients.Group(_clientsGroupIdFactory.GetLobbyGroupId(lobbyId));
    }
    
    public IHubByteSyncPush SessionGroupExcept(string sessionId, Client client)
    {
        return GroupExcept(_clientsGroupIdFactory.GetSessionGroupId(sessionId), client);
    }
    
    public IHubByteSyncPush LobbyGroupExcept(string lobbyId, Client client)
    {
        return GroupExcept(_clientsGroupIdFactory.GetLobbyGroupId(lobbyId), client);
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
}