using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Lobbies;

public class QuitLobbyRequest : IRequest<bool>
{
    public QuitLobbyRequest(string lobbyId, Client client)
    {
        LobbyId = lobbyId;
        Client = client;
    }
    
    public string LobbyId { get; }
    
    public Client Client { get; }
}