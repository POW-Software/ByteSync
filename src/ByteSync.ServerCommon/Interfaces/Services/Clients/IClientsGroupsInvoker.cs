using ByteSync.Common.Interfaces.Hub;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Business.Sessions;

namespace ByteSync.ServerCommon.Interfaces.Hubs;

public interface IClientsGroupsInvoker
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
}