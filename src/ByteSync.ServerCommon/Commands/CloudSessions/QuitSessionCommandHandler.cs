using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class QuitSessionCommandHandler : IRequestHandler<QuitSessionRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ICacheService _cacheService;
    private readonly ISessionMemberMapper _sessionMemberMapper;
    private readonly IByteSyncClientCaller _byteSyncClientCaller;

    public QuitSessionCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInventoryRepository inventoryRepository, 
        ISynchronizationRepository synchronizationRepository, ICacheService cacheService, ISessionMemberMapper sessionMemberMapper, 
        IByteSyncClientCaller byteSyncClientCaller)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _inventoryRepository = inventoryRepository;
        _synchronizationRepository = synchronizationRepository;
        _cacheService = cacheService;
        _sessionMemberMapper = sessionMemberMapper;
        _byteSyncClientCaller = byteSyncClientCaller;
    }
    
    public async Task Handle(QuitSessionRequest request, CancellationToken cancellationToken)
    {
        CloudSessionData? innerCloudSessionData = null;
        SessionMemberData? innerQuitter = null;

        var transaction = _cacheService.OpenTransaction();
        
        var updateSessionResult = await _cloudSessionsRepository.Update(request.SessionId, cloudSessionData =>
        {
            var quitter = cloudSessionData.SessionMembers.FirstOrDefault(m => m.ClientInstanceId.Equals(request.ClientInstanceId));
            
            if (quitter != null)
            {
                cloudSessionData.SessionMembers.Remove(quitter);

                if (cloudSessionData.SessionMembers.Count == 0)
                {
                    cloudSessionData.IsSessionRemoved = true;
                }

                innerCloudSessionData = cloudSessionData;
                innerQuitter = quitter;
            }

            return quitter != null;
        }, transaction);

        if (updateSessionResult.IsWaitingForTransaction)
        {
            await _inventoryRepository.UpdateIfExists(request.SessionId, inventoryData =>
            {
                var inventoryMember = inventoryData.InventoryMembers.SingleOrDefault(m => m.ClientInstanceId.Equals(request.ClientInstanceId));
                if (inventoryMember != null)
                {
                    inventoryData.InventoryMembers.Remove(inventoryMember);
                }
                
                inventoryData.RecodePathItems(innerCloudSessionData!);

                return true;
            }, transaction);
        }
        
        if (updateSessionResult.IsWaitingForTransaction)
        {
            await _synchronizationRepository.UpdateIfExists(request.SessionId, synchronizationData =>
            {
                if (innerCloudSessionData!.IsSessionActivated && !synchronizationData.IsEnded)
                {
                    synchronizationData.IsFatalError = true;

                    return true;
                }
                else
                {
                    return false;
                }
            }, transaction);
        }

        if (updateSessionResult.IsWaitingForTransaction)
        {       
            await transaction.ExecuteAsync();
        
            await _byteSyncClientCaller.RemoveFromGroup(request.Client, request.SessionId);
            var sessionMemberInfo = await _sessionMemberMapper.Convert(innerQuitter!);
            await _byteSyncClientCaller.SessionGroup(request.SessionId).MemberQuittedSession(sessionMemberInfo);
        }
    }
}