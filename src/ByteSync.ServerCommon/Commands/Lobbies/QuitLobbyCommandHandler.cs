using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Lobbies;

public class QuitLobbyCommandHandler : IRequestHandler<QuitLobbyRequest, bool>
{
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IClientsGroupsManager _clientsGroupsManager;
    private readonly IClientsGroupsInvoker _clientsGroupsInvoker;
    private readonly ILogger<QuitLobbyCommandHandler> _logger;
    
    public QuitLobbyCommandHandler(ILobbyRepository lobbyRepository, IClientsGroupsManager clientsGroupsManager, IClientsGroupsInvoker clientsGroupsInvoker,
        ILogger<QuitLobbyCommandHandler> logger)
    {
        _lobbyRepository = lobbyRepository;
        _clientsGroupsManager = clientsGroupsManager;
        _clientsGroupsInvoker = clientsGroupsInvoker;
        _logger = logger;
    }
    
    public async Task<bool> Handle(QuitLobbyRequest request, CancellationToken cancellationToken)
    {
        var lobbyId = request.LobbyId;
        var client = request.Client;
        
        var result = await _lobbyRepository.QuitLobby(lobbyId, client.ClientInstanceId);
        
        if (result.IsSaved)
        {
            _logger.LogInformation("QuitLobby: {member} quits {lobby}", client.ClientInstanceId, lobbyId);
        }
        else if (result.IsDeleted)
        {
            _logger.LogInformation("QuitLobby: {member} quits {lobby}", client.ClientInstanceId, lobbyId);
            _logger.LogInformation("QuitLobby: {lobby} is closed", lobbyId);
        }
        else
        {
            _logger.LogWarning("QuitLobby: {member} not found in {lobby}", client.ClientInstanceId, lobbyId);
        }

        if (result.IsSaved || result.IsDeleted)
        {
            await _clientsGroupsInvoker.LobbyGroup(lobbyId).
                MemberQuittedLobby(lobbyId, client.ClientInstanceId).ConfigureAwait(false);
                
            await _clientsGroupsManager.RemoveFromLobbyGroup(client, $"Lobby_{lobbyId}").ConfigureAwait(false);
        }

        return result.IsSaved || result.IsDeleted;
    }
}