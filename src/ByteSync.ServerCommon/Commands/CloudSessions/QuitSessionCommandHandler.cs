using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Mappers;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class QuitSessionCommandHandler : IRequestHandler<QuitSessionRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ICacheService _cacheService;
    private readonly ISessionMemberMapper _sessionMemberMapper;
    private readonly IClientsGroupsService _clientsGroupsService;
    private readonly IInvokeClientsService _invokeClientsService;

    public QuitSessionCommandHandler(ICloudSessionsRepository cloudSessionsRepository, IInventoryRepository inventoryRepository, 
        ISynchronizationRepository synchronizationRepository, ICacheService cacheService, ISessionMemberMapper sessionMemberMapper, 
        IClientsGroupsService clientsGroupsService, IInvokeClientsService invokeClientsService)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _inventoryRepository = inventoryRepository;
        _synchronizationRepository = synchronizationRepository;
        _cacheService = cacheService;
        _sessionMemberMapper = sessionMemberMapper;
        _clientsGroupsService = clientsGroupsService;
        _invokeClientsService = invokeClientsService;
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
            await _clientsGroupsService.RemoveSessionSubscription(request.Client, request.SessionId, transaction);
            
            await transaction.ExecuteAsync();
        
            await _clientsGroupsService.RemoveFromSessionGroup(request.Client, request.SessionId);
            
            var sessionMemberInfo = await _sessionMemberMapper.Convert(innerQuitter!);
            await _invokeClientsService.SessionGroup(request.SessionId).MemberQuittedSession(sessionMemberInfo);
        }
    }
}