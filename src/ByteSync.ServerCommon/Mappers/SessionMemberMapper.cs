using ByteSync.Common.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;

namespace ByteSync.ServerCommon.Mappers;

public class SessionMemberMapper : ISessionMemberMapper
{
    private readonly IClientsRepository _clientsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IByteSyncEndpointFactory _byteSyncEndpointFactory;

    public SessionMemberMapper(IClientsRepository clientsRepository, IInventoryRepository inventoryRepository,
        IByteSyncEndpointFactory byteSyncEndpointFactory)
    {
        _clientsRepository = clientsRepository;
        _inventoryRepository = inventoryRepository;
        _byteSyncEndpointFactory = byteSyncEndpointFactory;
    }

    public async Task<SessionMemberInfoDTO> Convert(SessionMemberData sessionMemberData)
    {
        var client = await _clientsRepository.Get(sessionMemberData.ClientInstanceId);
        
        var endpoint = _byteSyncEndpointFactory.BuildByteSyncEndpoint(client!, null);
        
        var inventoryMember = await _inventoryRepository.GetInventoryMember(sessionMemberData.CloudSessionData.SessionId, 
            sessionMemberData.ClientInstanceId);
        
        SessionMemberInfoDTO sessionMemberInfo = new SessionMemberInfoDTO
        {
            Endpoint = endpoint,
            EncryptedPrivateData = sessionMemberData.EncryptedPrivateData!,
            ProfileClientId = sessionMemberData.ProfileClientId,
            SessionId = sessionMemberData.CloudSessionData.SessionId,
            LobbyId = sessionMemberData.CloudSessionData.LobbyId,
            JoinedSessionOn = sessionMemberData.JoinedSessionOn,
            PositionInList = sessionMemberData.PositionInList,
            SessionMemberGeneralStatus = inventoryMember?.SessionMemberGeneralStatus ?? SessionMemberGeneralStatus.InventoryWaitingForStart
        };

        return sessionMemberInfo;
    }
}