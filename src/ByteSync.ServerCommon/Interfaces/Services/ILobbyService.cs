using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ILobbyService
{
    // Task<LobbyInfo?> GetLobby(string lobbyId);
    
    Task<JoinLobbyResult> TryJoinLobby(JoinLobbyParameters joinLobbyParameters, Client client);

    // todo 270523: no longer called by ClientsManager
    // Task QuitAllLobbies(Client client);
    
    Task<bool> QuitLobby(string lobbyId, Client byteSyncEndpoint);
    
    // Task<bool> LobbyMemberExists(string lobbyId, string clientInstanceId);
    //
    // Task<bool> IsLobbyFirstMember(string credentialsLobbyId, string clientInstanceId);
    
    Task<LobbyMemberInfo?> UpdateLobbyMemberStatus(string lobbyId, Client client, LobbyMemberStatuses lobbyMemberStatus);

    Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials lobbyCloudSessionCredentials, Client client); 
    
    Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo, Client client);
}