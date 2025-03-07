using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Lobbies;

public class TryJoinLobbyRequest : IRequest<JoinLobbyResult>
{
    public TryJoinLobbyRequest(JoinLobbyParameters joinLobbyParameters, Client client)
    {
        JoinLobbyParameters = joinLobbyParameters;
        Client = client;
    }

    public JoinLobbyParameters JoinLobbyParameters { get; set; }
    
    public Client Client { get; set; }
}