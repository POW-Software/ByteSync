using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Helpers;
using ByteSync.ServerCommon.Interfaces.Hubs;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class StartInventoryCommandHandler : IRequestHandler<StartInventoryRequest, StartInventoryResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly IByteSyncClientCaller _byteSyncClientCaller;
    private readonly ICacheService _cacheService;
    private readonly ILogger<StartInventoryCommandHandler> _logger;
    
    public StartInventoryCommandHandler(
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        IByteSyncClientCaller byteSyncClientCaller,
        ICacheService cacheService,
        ILogger<StartInventoryCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _byteSyncClientCaller = byteSyncClientCaller;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<StartInventoryResult> Handle(StartInventoryRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        
        await using var sessionRedisLock = await _cacheService.AcquireLockAsync(_cloudSessionsRepository.ComputeCacheKey(_cloudSessionsRepository.ElementName, 
            sessionId));
        
        await using var inventoryRedisLock = await _cacheService.AcquireLockAsync(_inventoryRepository.ComputeCacheKey(_inventoryRepository.ElementName, 
            sessionId));
        
        var transaction = _cacheService.OpenTransaction();
        
        StartInventoryResult? startInventoryResult = null;
        CloudSessionData? cloudSessionData = null;
        UpdateEntityResult<InventoryData>? inventoryUpdateResult = null;
        
        var sessionUpdateResult = await _cloudSessionsRepository.UpdateIfExists(sessionId, session =>
        {
            if (!session.IsSessionActivated)
            {
                session.IsSessionActivated = true;
                cloudSessionData = session;
                return true;
            }
            else
            {
                _logger.LogWarning("Session {sessionId} is already activated", sessionId);
                return false;
            }
        }, transaction, sessionRedisLock);
        
        if (cloudSessionData == null)
        {
            _logger.LogInformation("StartInventory: session {@sessionId}: not found", sessionId);
            return StartInventoryResult.BuildFrom(StartInventoryStatuses.SessionNotFound);
        }

        if (sessionUpdateResult.IsWaitingForTransaction)
        {
            inventoryUpdateResult = await _inventoryRepository.Update(sessionId, inventoryData =>
            {
                if (cloudSessionData!.SessionMembers.Count < 2)
                {
                    startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.LessThan2Members);
                }
                else if (cloudSessionData.SessionMembers.Count > 5)
                {
                    startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.MoreThan5Members);
                }
                else if (inventoryData.InventoryMembers.Count != cloudSessionData.SessionMembers.Count)
                {
                    startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.UnknownError);
                }
                else
                {
                    if (inventoryData.InventoryMembers.Any(imd => imd.SharedPathItems.Count == 0))
                    {
                        startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, 
                            StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
                    }
                }

                if (startInventoryResult == null)
                {
                    inventoryData.IsInventoryStarted = true;
                    startInventoryResult = StartInventoryResult.BuildOK();
                }
            
                return startInventoryResult.IsOK;
            }, transaction, inventoryRedisLock);
        }

        if (sessionUpdateResult.IsWaitingForTransaction 
            && inventoryUpdateResult != null && inventoryUpdateResult.IsWaitingForTransaction 
            && startInventoryResult!.IsOK)
        {
            await transaction.ExecuteAsync();
            
            await _sharedFilesService.ClearSession(sessionId);
            
            var dto = new InventoryStartedDTO(sessionId, client.ClientInstanceId, cloudSessionData!.SessionSettings);
            await _byteSyncClientCaller.SessionGroupExcept(sessionId, client).InventoryStarted(dto);
            
            _logger.LogInformation("StartInventory: session {@cloudSession} - OK", sessionId);
        }

        return startInventoryResult!;
    }
    
    private StartInventoryResult LogAndBuildStartInventoryResult(CloudSessionData cloudSessionData, StartInventoryStatuses status)
    {
        _logger.LogInformation("StartInventory: session {@cloudSession} - {Status}", cloudSessionData.BuildLog(), status);
        return StartInventoryResult.BuildFrom(status);
    }
}