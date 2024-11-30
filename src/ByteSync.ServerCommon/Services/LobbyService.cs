using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class LobbyService : ILobbyService
{
    private readonly ILobbyRepository _lobbyRepository;
    private readonly ICloudSessionProfileRepository _cloudSessionProfileRepository;
    private readonly ILobbyFactory _lobbyFactory;
    private readonly IByteSyncClientCaller _byteSyncClientCaller;
    private readonly ILogger<LobbyService> _logger;
    
    public LobbyService(ILobbyRepository lobbyRepository, ICloudSessionProfileRepository cloudSessionProfileRepository, 
        ILobbyFactory lobbyFactory, IByteSyncClientCaller byteSyncClientCaller, ILogger<LobbyService> logger)
    {
        _lobbyRepository = lobbyRepository;
        _cloudSessionProfileRepository = cloudSessionProfileRepository;
        _lobbyFactory = lobbyFactory;
        _byteSyncClientCaller = byteSyncClientCaller;
        _logger = logger;
    }
    
    public async Task<JoinLobbyResult> TryJoinLobby(JoinLobbyParameters joinLobbyParameters, Client client)
    {
        JoinLobbyResult? joinLobbyResult = null;
        bool? isConnected = null;
        
        // string? lobbyId = null;
        CloudSessionProfileEntity? cloudSessionProfile = null;

        var updateResult1 = await _cloudSessionProfileRepository.AddOrUpdate(joinLobbyParameters.CloudSessionProfileId, cloudSessionProfileEntity =>
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
        });

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
                // On contrôle le JoinMode
                int index = cloudSessionProfile.Slots.FindIndex(s => s.ProfileClientId == joinLobbyParameters.ProfileClientId);
                bool isJoinModeOK;
                if (index == 0)
                {
                    isJoinModeOK = joinLobbyParameters.JoinMode.In(JoinLobbyModes.RunInventory, JoinLobbyModes.RunSynchronization);
                    // lobby.SetSessionMode(joinLobbyParameters.JoinMode);
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
        });
        
        if (updateResult.IsSaved)
        {
            LobbyInfo lobbyInfo = updateResult.Element!.BuildLobbyInfo();
            
            _logger.LogInformation("TryJoinLobby: {@joiner} joins lobby {LobbyId}", client.BuildLog(), lobbyInfo.LobbyId);
            
            joinLobbyResult = JoinLobbyResult.BuildFrom(lobbyInfo, 
                isConnected!.Value ? JoinLobbyStatuses.LobbyJoinedSucessfully : JoinLobbyStatuses.LobbyPreviouslyJoined);
            
            var memberInfo = lobbyInfo.GetMember(joinLobbyParameters.ProfileClientId)!;

            await _byteSyncClientCaller
                .LobbyGroupExcept(lobbyInfo.LobbyId, client)
                .MemberJoinedLobby(lobbyInfo.LobbyId, memberInfo)
                .ConfigureAwait(false);

            await _byteSyncClientCaller
                .AddToLobbyGroup(client, lobbyInfo.LobbyId)
                .ConfigureAwait(false);
        }

        return joinLobbyResult!;
    }

    public async Task<LobbyMemberInfo?> UpdateLobbyMemberStatus(string lobbyId, Client client, LobbyMemberStatuses lobbyMemberStatus)
    {
        LobbyMember? lobbyMember = null;
        
        var result = await _lobbyRepository.Update(lobbyId, lobby =>
        {
            lobbyMember = lobby.ConnectedLobbyMembers.SingleOrDefault(m => m.ClientInstanceId.Equals(client.ClientInstanceId));
            
            if (lobbyMember != null)
            {
                lobbyMember.Status = lobbyMemberStatus;
                

                
                return true;
            }
            else
            {
                return false;
            }
        });

        if (result.IsSaved)
        {
            await _byteSyncClientCaller.LobbyGroupExcept(lobbyId, client)
                .LobbyMemberStatusUpdated(lobbyId, client.ClientInstanceId, lobbyMemberStatus).ConfigureAwait(false);
        }
        
        var lobbyMemberInfo = lobbyMember?.GetLobbyMemberInfo();
        
        return lobbyMemberInfo;
    }

    public async Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials lobbyCloudSessionCredentials, Client client)
    {
        var lobby = await _lobbyRepository.Get(lobbyCloudSessionCredentials.LobbyId);

        if (lobby != null)
        {
            var firstCell = lobby.LobbyMemberCells.FirstOrDefault();
            if (firstCell is { LobbyMember: not null } && firstCell.LobbyMember.ClientInstanceId .Equals(client.ClientInstanceId))
            {
                _logger.LogInformation("SendLobbyCloudSessionCredentials: {@credentials} sent to {@recipient}", 
                    lobbyCloudSessionCredentials, lobbyCloudSessionCredentials.Recipient);
                
                await _byteSyncClientCaller.Client(lobbyCloudSessionCredentials.Recipient)
                    .LobbyCloudSessionCredentialsSent(lobbyCloudSessionCredentials).ConfigureAwait(false);
            }
        }
    }

    public async Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo, Client client)
    {
        var lobby = await _lobbyRepository.Get(lobbyCheckInfo.LobbyId);
        
        if (lobby != null && lobby.LobbyMemberCells.Any(c => c.LobbyMember != null && c.LobbyMember.ClientInstanceId == client.ClientInstanceId))
        {
            await _byteSyncClientCaller.LobbyGroup(lobbyCheckInfo.LobbyId)
                .LobbyCheckInfosSent(lobbyCheckInfo.LobbyId, lobbyCheckInfo).ConfigureAwait(false);
        }
    }

    public async Task<bool> QuitLobby(string lobbyId, Client client)
    {
        var result = await _lobbyRepository.QuitLobby(lobbyId, client.ClientInstanceId);
        
        if (result.IsSaved)
        {
            _logger.LogInformation("QuitLobby: {@member} quits {@lobby}", client.BuildLog(), lobbyId);
        }
        else if (result.IsDeleted)
        {
            _logger.LogInformation("QuitLobby: {@member} quits {@lobby}", client.BuildLog(), lobbyId);
            _logger.LogInformation("QuitLobby: {@lobby} is closed", lobbyId);
        }
        else
        {
            _logger.LogWarning("QuitLobby: {@member} not found in {@lobby}", client.BuildLog(), lobbyId);
        }

        if (result.IsSaved || result.IsDeleted)
        {
            await _byteSyncClientCaller.LobbyGroup(lobbyId).
                MemberQuittedLobby(lobbyId, client.ClientInstanceId).ConfigureAwait(false);
                
            await _byteSyncClientCaller.RemoveFromGroup(client, $"Lobby_{lobbyId}").ConfigureAwait(false);
        }

        return result.IsSaved || result.IsDeleted;
    }
}