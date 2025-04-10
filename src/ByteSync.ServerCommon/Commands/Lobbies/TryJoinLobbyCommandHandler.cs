using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Lobbies;

public class TryJoinLobbyCommandHandler : IRequestHandler<TryJoinLobbyRequest, JoinLobbyResult>
{
    private readonly ICloudSessionProfileRepository _cloudSessionProfileRepository;
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly IClientsGroupsService _clientsGroupsService;
    private readonly ILobbyFactory _lobbyFactory;
    private readonly IRedisInfrastructureService _redisInfrastructureService;
    private readonly ILogger<TryJoinLobbyCommandHandler> _logger;

    public TryJoinLobbyCommandHandler(ICloudSessionProfileRepository cloudSessionProfileRepository, ILobbyRepository lobbyRepository, 
        IInvokeClientsService invokeClientsService, IClientsGroupsService clientsGroupsService, ILobbyFactory lobbyFactory,
        IRedisInfrastructureService redisInfrastructureService, ILogger<TryJoinLobbyCommandHandler> logger)
    {
        _cloudSessionProfileRepository = cloudSessionProfileRepository;
        _lobbyRepository = lobbyRepository;
        _invokeClientsService = invokeClientsService;
        _clientsGroupsService = clientsGroupsService;
        _lobbyFactory = lobbyFactory;
        _redisInfrastructureService = redisInfrastructureService;
        _logger = logger;
    }
    
    public async Task<JoinLobbyResult> Handle(TryJoinLobbyRequest request, CancellationToken cancellationToken)
    {
        var joinLobbyParameters = request.JoinLobbyParameters;
        var client = request.Client;
        
        JoinLobbyResult? joinLobbyResult = null;
        bool? isConnected = null;
        
        var transaction = _redisInfrastructureService.OpenTransaction();
        
        CloudSessionProfileEntity? cloudSessionProfile = null;

        await _cloudSessionProfileRepository.AddOrUpdate(joinLobbyParameters.CloudSessionProfileId, cloudSessionProfileEntity =>
        {
            if (cloudSessionProfileEntity == null)
            {
                joinLobbyResult = JoinLobbyResult.BuildFrom(JoinLobbyStatuses.UnknownCloudSessionProfile);
                return null;
            }
            else
            {
                cloudSessionProfile = cloudSessionProfileEntity;
                
                if (cloudSessionProfileEntity.CurrentLobbyId == null)
                {
                    Lobby lobby = _lobbyFactory.BuildLobby(cloudSessionProfileEntity);
                    _lobbyRepository.Save(lobby.LobbyId, lobby);
                    
                    cloudSessionProfileEntity.CurrentLobbyId = lobby.LobbyId;

                    return cloudSessionProfileEntity;
                }
                else
                {
                    return null;
                }
            }
        }, transaction);

        if (joinLobbyResult != null)
        {
            return joinLobbyResult;
        }
        
        var updateResult = await _lobbyRepository.AddOrUpdate(cloudSessionProfile!.CurrentLobbyId!, lobby =>
        {
            if (lobby == null)
            {
                joinLobbyResult = JoinLobbyResult.BuildFrom(JoinLobbyStatuses.UnknownCloudSessionProfile);
                return null;
            }

            if (cloudSessionProfile.Slots.Any(s => s.ProfileClientId == joinLobbyParameters.ProfileClientId))
            {
                int index = cloudSessionProfile.Slots.FindIndex(s => s.ProfileClientId == joinLobbyParameters.ProfileClientId);
                bool isJoinModeOK;
                if (index == 0)
                {
                    isJoinModeOK = joinLobbyParameters.JoinMode.In(JoinLobbyModes.RunInventory, JoinLobbyModes.RunSynchronization);
                }
                else
                {
                    isJoinModeOK = joinLobbyParameters.JoinMode.In(JoinLobbyModes.Join);
                }

                if (!isJoinModeOK)
                {
                    joinLobbyResult = JoinLobbyResult.BuildFrom(JoinLobbyStatuses.UnexpectedLobbyJoinMode);
                    return null;
                }

                isConnected = lobby.ConnectLobbyMember(joinLobbyParameters.ProfileClientId, joinLobbyParameters.PublicKeyInfo, 
                    joinLobbyParameters.JoinMode, client);
                
                return lobby;
            }
            else
            {
                joinLobbyResult = JoinLobbyResult.BuildFrom(JoinLobbyStatuses.UnknownProfileClientId);
                return null;
            }
        }, transaction);
        
        if (updateResult.IsWaitingForTransaction)
        {
            LobbyInfo lobbyInfo = updateResult.Element!.BuildLobbyInfo();
            
            _logger.LogInformation("TryJoinLobby: {@joiner} joins lobby {LobbyId}", client.BuildLog(), lobbyInfo.LobbyId);
            
            joinLobbyResult = JoinLobbyResult.BuildFrom(lobbyInfo, 
                isConnected!.Value ? JoinLobbyStatuses.LobbyJoinedSucessfully : JoinLobbyStatuses.LobbyPreviouslyJoined);
            
            var memberInfo = lobbyInfo.GetMember(joinLobbyParameters.ProfileClientId)!;

            await _clientsGroupsService.AddLobbySubscription(client, lobbyInfo.LobbyId, transaction);

            await transaction.ExecuteAsync();
            
            await _invokeClientsService.LobbyGroupExcept(lobbyInfo.LobbyId, client).MemberJoinedLobby(lobbyInfo.LobbyId, memberInfo)
                .ConfigureAwait(false);

            await _clientsGroupsService.AddToLobbyGroup(client, lobbyInfo.LobbyId)
                .ConfigureAwait(false);
        }

        return joinLobbyResult!;
    }
}