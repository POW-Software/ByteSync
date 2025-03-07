using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Lobbies;

public class QuitLobbyCommandHandler : IRequestHandler<QuitLobbyRequest, bool>
{
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IClientsGroupsManager _clientsGroupsManager;
    private readonly IClientsGroupsInvoker _clientsGroupsInvoker;    
    private readonly ICacheService _cacheService;
    private readonly IClientsRepository _clientsRepository;
    private readonly ILogger<QuitLobbyCommandHandler> _logger;
    
    public QuitLobbyCommandHandler(ILobbyRepository lobbyRepository, IClientsGroupsManager clientsGroupsManager, IClientsGroupsInvoker clientsGroupsInvoker,
        ICacheService cacheService, IClientsRepository clientsRepository, ILogger<QuitLobbyCommandHandler> logger)
    {
        _lobbyRepository = lobbyRepository;
        _clientsGroupsManager = clientsGroupsManager;
        _clientsGroupsInvoker = clientsGroupsInvoker;
        _cacheService = cacheService;
        _clientsRepository = clientsRepository;
        _logger = logger;
    }
    
    public async Task<bool> Handle(QuitLobbyRequest request, CancellationToken cancellationToken)
    {
        var lobbyId = request.LobbyId;
        var client = request.Client;

        var transaction = _cacheService.OpenTransaction();

        var result = await _lobbyRepository.QuitLobby(lobbyId, client.ClientInstanceId, transaction);
        
        if (result.IsWaitingForTransaction)
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

        if (result.IsWaitingForTransaction || result.IsDeleted)
        {
            await _clientsRepository.RemoveLobbySubscription(client, lobbyId, transaction);

            await transaction.ExecuteAsync();
            
            await _clientsGroupsInvoker.LobbyGroup(lobbyId).
                MemberQuittedLobby(lobbyId, client.ClientInstanceId).ConfigureAwait(false);
                
            await _clientsGroupsManager.RemoveFromLobbyGroup(client, $"Lobby_{lobbyId}").ConfigureAwait(false);
        }

        return result.IsSaved || result.IsDeleted;
    }
}