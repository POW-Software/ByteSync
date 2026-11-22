using ByteSync.Common.Business.Lobbies;
using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services;

public class LobbyService : ILobbyService
{
    private readonly ILobbyRepository _lobbyRepository;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly ILogger<LobbyService> _logger;
    
    public LobbyService(ILobbyRepository lobbyRepository, IInvokeClientsService invokeClientsService, ILogger<LobbyService> logger)
    {
        _lobbyRepository = lobbyRepository;
        _invokeClientsService = invokeClientsService;
        _logger = logger;
    }

    public async Task<LobbyMemberInfo?> UpdateLobbyMemberStatus(string lobbyId, Client client, LobbyMemberStatuses lobbyMemberStatus)
    {
        LobbyMember? lobbyMember = null;
        
        var result = await _lobbyRepository.Update(lobbyId, lobby =>
        {
            if (lobby.AuthenticationEpoch != ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch) throw new UnauthorizedAccessException();
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
            await _invokeClientsService.LobbyGroupExcept(lobbyId, client)
                .LobbyMemberStatusUpdated(lobbyId, client.ClientInstanceId, lobbyMemberStatus).ConfigureAwait(false);
        }
        
        var lobbyMemberInfo = lobbyMember?.GetLobbyMemberInfo();
        
        return lobbyMemberInfo;
    }

    public async Task SendLobbyCloudSessionCredentials(LobbyCloudSessionCredentials lobbyCloudSessionCredentials, Client client)
    {
        var lobby = await _lobbyRepository.Get(lobbyCloudSessionCredentials.LobbyId);

        if (lobby?.AuthenticationEpoch == ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch)
        {
            var firstCell = lobby.LobbyMemberCells.FirstOrDefault();
            if (firstCell is { LobbyMember: not null } && firstCell.LobbyMember.ClientInstanceId .Equals(client.ClientInstanceId))
            {
                if (lobby.GetLobbyMemberByClientInstanceId(lobbyCloudSessionCredentials.Recipient) == null) throw new UnauthorizedAccessException();
                _logger.LogInformation("Lobby credentials sent to {Recipient}", lobbyCloudSessionCredentials.Recipient);
                
                await _invokeClientsService.Client(lobbyCloudSessionCredentials.Recipient)
                    .LobbyCloudSessionCredentialsSent(lobbyCloudSessionCredentials).ConfigureAwait(false);
            }
        }
    }

    public async Task SendLobbyCheckInfos(LobbyCheckInfo lobbyCheckInfo, Client client)
    {
        var lobby = await _lobbyRepository.Get(lobbyCheckInfo.LobbyId);
        
        if (lobby?.AuthenticationEpoch == ByteSync.Common.Business.Auth.AuthenticationProtocol.Epoch && lobby.LobbyMemberCells.Any(c => c.LobbyMember != null && c.LobbyMember.ClientInstanceId == client.ClientInstanceId))
        {
            await _invokeClientsService.LobbyGroup(lobbyCheckInfo.LobbyId)
                .LobbyCheckInfosSent(lobbyCheckInfo.LobbyId, lobbyCheckInfo).ConfigureAwait(false);
        }
    }
}