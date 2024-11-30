using System.Threading.Tasks;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ILobbyApiClient
{
    Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials credentials);
    
    Task<JoinLobbyResult> JoinLobby(JoinLobbyParameters joinLobbyParameters);
    
    Task QuitLobby(string lobbyId);
    
    Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo);
    
    Task UpdateLobbyMemberStatus(string lobbyId, LobbyMemberStatuses status);
}