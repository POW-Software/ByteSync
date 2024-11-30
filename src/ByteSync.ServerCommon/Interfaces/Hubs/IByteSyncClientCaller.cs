using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Hubs;

public interface IByteSyncClientCaller
{
    IHubByteSyncPush Client(string clientInstanceId);
    
    IHubByteSyncPush Client(Client client);
    
    IHubByteSyncPush Client(SessionMemberData sessionMemberData);
    
    IHubByteSyncPush Clients(ICollection<string> clientInstanceIds);
    
    IHubByteSyncPush Clients(ICollection<SessionMemberData> sessionMemberDatas);
    
    IHubByteSyncPush SessionGroup(string sessionId);
    
    IHubByteSyncPush LobbyGroup(string lobbyId);
    
    IHubByteSyncPush SessionGroupExcept(string sessionId, Client client);
    
    IHubByteSyncPush LobbyGroupExcept(string lobbyId, Client client);
    
    Task AddToSessionGroup(Client client, string sessionId);
    
    Task AddToLobbyGroup(Client client, string lobby);
    
    Task RemoveFromGroup(Client client, string groupName);
    
    Task AddClientGroup(string connectionId, Client client);
    
    Task RemoveClientGroup(string connectionId, Client client);
}