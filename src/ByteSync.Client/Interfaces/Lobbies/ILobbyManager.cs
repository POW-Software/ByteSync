using System.Threading.Tasks;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;

namespace ByteSync.Interfaces.Lobbies;

public interface ILobbyManager
{
    Task StartLobbyAsync(AbstractSessionProfile sessionProfile, JoinLobbyModes joinLobbyMode);
    
    Task ShowProfileDetails(AbstractSessionProfile sessionProfile);
    
    Task ExitLobby(string lobbyId);
    
    Task RunSecurityCheckAsync(string lobbyId);
    
    Task OnLobbyCloudSessionCredentialsSent(LobbyCloudSessionCredentials lobbyCloudSessionCredentials);
}