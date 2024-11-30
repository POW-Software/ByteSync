using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ILobbyService
{
    Task<JoinLobbyResult> TryJoinLobby(JoinLobbyParameters joinLobbyParameters, Client client);
    
    Task<bool> QuitLobby(string lobbyId, Client byteSyncEndpoint);
    
    Task<LobbyMemberInfo?> UpdateLobbyMemberStatus(string lobbyId, Client client, LobbyMemberStatuses lobbyMemberStatus);

    Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials lobbyCloudSessionCredentials, Client client); 
    
    Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo, Client client);
}