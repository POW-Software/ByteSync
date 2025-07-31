using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using ByteSync.ServerCommon.Interfaces.Services.Clients;
using MediatR;
using Microsoft.Extensions.Logging;
using ByteSync.ServerCommon.Entities.Inventories;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Commands.Inventories;

public class StartInventoryCommandHandler : IRequestHandler<StartInventoryRequest, StartInventoryResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly IInvokeClientsService _invokeClientsService;
    private readonly IRedisInfrastructureService _redisInfrastructureService;
    private readonly ILogger<StartInventoryCommandHandler> _logger;
    
    public StartInventoryCommandHandler(
        IInventoryRepository inventoryRepository,
        ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        IInvokeClientsService invokeClientsService,
        IRedisInfrastructureService redisInfrastructureService,
        ILogger<StartInventoryCommandHandler> logger)
    {
        _inventoryRepository = inventoryRepository;
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _invokeClientsService = invokeClientsService;
        _redisInfrastructureService = redisInfrastructureService;
        _logger = logger;
    }
    
    public async Task<StartInventoryResult> Handle(StartInventoryRequest request, CancellationToken cancellationToken)
    {
        var sessionId = request.SessionId;
        var client = request.Client;
        
        await using var sessionRedisLock = await _redisInfrastructureService.AcquireLockAsync(EntityType.Session, sessionId);
        
        await using var inventoryRedisLock = await _redisInfrastructureService.AcquireLockAsync(EntityType.Inventory, sessionId);
        
        var transaction = _redisInfrastructureService.OpenTransaction();
        
        UpdateEntityResult<InventoryEntity>? inventoryUpdateResult = null;
        
        var sessionUpdateResult = await ActivateSession(sessionId, transaction, sessionRedisLock);

        var startInventoryResult = CheckSession(sessionId, sessionUpdateResult);
        if (startInventoryResult != null)
        {
            return startInventoryResult;
        }
        
        if (sessionUpdateResult.IsWaitingForTransaction)
        {
            (inventoryUpdateResult, startInventoryResult) = 
                await UpdateInventory(sessionUpdateResult, transaction, inventoryRedisLock);
        }

        if (sessionUpdateResult.IsWaitingForTransaction 
            && inventoryUpdateResult != null && inventoryUpdateResult.IsWaitingForTransaction)
        {
            await transaction.ExecuteAsync();
            
            await _sharedFilesService.ClearSession(sessionId);
            
            var dto = new InventoryStartedDTO(sessionId, client.ClientInstanceId, sessionUpdateResult.Element!.SessionSettings);
            await _invokeClientsService.SessionGroupExcept(sessionId, client).InventoryStarted(dto);
            
            _logger.LogInformation("StartInventory: session {SessionId} - OK", sessionId);
        }

        return startInventoryResult!;
    }

    private StartInventoryResult? CheckSession(string sessionId, UpdateEntityResult<CloudSessionData> sessionUpdateResult)
    {
        StartInventoryResult? startInventoryResult = null;
        
        if (sessionUpdateResult.IsNoOperation)
        {
            startInventoryResult = LogAndBuildStartInventoryResult(sessionId, StartInventoryStatuses.InventoryStartedSucessfully, "already activated");
        }
        else if (sessionUpdateResult.IsNotFound)
        {
            startInventoryResult = LogAndBuildStartInventoryResult(sessionId, StartInventoryStatuses.SessionNotFound);
        }
        else
        {
            var cloudSessionData = sessionUpdateResult.Element!;
            if (cloudSessionData.SessionMembers.Count > 5)
            {
                startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.MoreThan5Members);
            }
        }
        
        return startInventoryResult;
    }

    private async Task<(UpdateEntityResult<InventoryEntity> inventoryUpdateResult, StartInventoryResult? startInventoryResult)> UpdateInventory( 
        UpdateEntityResult<CloudSessionData> sessionUpdateResult, ITransaction transaction, IRedLock inventoryRedisLock)
    {
        StartInventoryResult? startInventoryResult = null;
        var cloudSessionData = sessionUpdateResult.Element!;
        
        UpdateEntityResult<InventoryEntity> inventoryUpdateResult;
        inventoryUpdateResult = await _inventoryRepository.UpdateIfExists(cloudSessionData.SessionId, inventoryData =>
        {
            if (inventoryData.InventoryMembers.Count > cloudSessionData.SessionMembers.Count)
            {
                startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.UnknownError);
            }
            else if (inventoryData.InventoryMembers.Count < cloudSessionData.SessionMembers.Count)
            {
                startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
            }
            else if (inventoryData.InventoryMembers.Any(imd => imd.DataNodes.Count == 0))
            {
                startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.UnknownError);
            }
            else if (inventoryData.InventoryMembers.Any(imd => imd.DataNodes.Any(dn => dn.DataSources.Count == 0)))
            {
                startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
            }
            else
            {
                var totalDataNodes = inventoryData.InventoryMembers.Sum(imd => imd.DataNodes.Count);
                if (totalDataNodes < 2)
                {
                    startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.LessThan2DataNodes);
                }
                else if (totalDataNodes > 5)
                {
                    startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, StartInventoryStatuses.MoreThan5DataNodes);
                }
            }

            if (startInventoryResult == null)
            {
                inventoryData.IsInventoryStarted = true;
                startInventoryResult = StartInventoryResult.BuildOK();
            }
            
            return startInventoryResult.IsOK;
        }, transaction, inventoryRedisLock);

        if (inventoryUpdateResult.IsNotFound)
        {
            startInventoryResult = LogAndBuildStartInventoryResult(cloudSessionData, 
                StartInventoryStatuses.AtLeastOneMemberWithNoDataToSynchronize);
        }
        
        return (inventoryUpdateResult, startInventoryResult);
    }

    private async Task<UpdateEntityResult<CloudSessionData>> ActivateSession(string sessionId, ITransaction transaction, IRedLock sessionRedisLock)
    {
        var sessionUpdateResult = await _cloudSessionsRepository.UpdateIfExists(sessionId, session =>
        {
            if (!session.IsSessionActivated)
            {
                session.IsSessionActivated = true;
                return true;
            }
            else
            {
                _logger.LogWarning("Session {sessionId} is already activated", sessionId);
                return false;
            }
        }, transaction, sessionRedisLock);
        
        return sessionUpdateResult;
    }
    
    private StartInventoryResult LogAndBuildStartInventoryResult(CloudSessionData cloudSessionData, StartInventoryStatuses status, 
        string? details = null)
    {
        return LogAndBuildStartInventoryResult(cloudSessionData.SessionId, status, details);
    }
    
    private StartInventoryResult LogAndBuildStartInventoryResult(string sessionId, StartInventoryStatuses status, string? details = null)
    {
        if (details != null)
        {
            _logger.LogInformation("StartInventory: session {SessionId} - {Status} - {Details}", sessionId, status, details);
        }
        else
        {
            _logger.LogInformation("StartInventory: session {SessionId} - {Status}", sessionId, status);
        }
        return StartInventoryResult.BuildFrom(status);
    }
}