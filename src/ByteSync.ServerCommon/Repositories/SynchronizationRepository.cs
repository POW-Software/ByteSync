using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class SynchronizationRepository : BaseRepository<SynchronizationEntity>, ISynchronizationRepository
{
    private readonly IActionsGroupDefinitionsRepository _actionsGroupDefinitionsRepository;
    private readonly ICacheRepository<TrackingActionEntity> _cacheTrackingAction;
    
    public SynchronizationRepository(IRedisInfrastructureService redisInfrastructureService, ICacheRepository<SynchronizationEntity> cacheRepository,
        IActionsGroupDefinitionsRepository actionsGroupDefinitionsRepository, ICacheRepository<TrackingActionEntity> cacheTrackingAction) : base(redisInfrastructureService, cacheRepository)
    {
        _actionsGroupDefinitionsRepository = actionsGroupDefinitionsRepository;
        _cacheTrackingAction = cacheTrackingAction;
    }

    public override EntityType EntityType => EntityType.Synchronization;

    public async Task AddSynchronization(SynchronizationEntity synchronizationEntity, List<ActionsGroupDefinition> actionsGroupDefinitions)
    {
        var synchronizationCacheKey = _cacheService.ComputeCacheKey(EntityType.Synchronization, synchronizationEntity.SessionId);
        await using var synchronizationLock = await _cacheService.AcquireLockAsync(synchronizationCacheKey);
        
        await Save(synchronizationEntity.SessionId, synchronizationEntity, null, synchronizationLock);
        
        // foreach (var groupDefinition in actionsGroupDefinitions)
        // {
        //     var trackingActionEntity = new TrackingActionEntity
        //     {
        //         ActionsGroupId = groupDefinition.ActionsGroupId,
        //         SourceClientInstanceId = groupDefinition.Source,
        //         TargetClientInstanceIds = [..groupDefinition.Targets],
        //         Size = groupDefinition.Size,
        //     };
        //     
        //     var cacheKey = _redisInfrastructureService2.ComputeCacheKey(EntityType.TrackingAction, $"{synchronizationEntity.SessionId}_{groupDefinition.ActionsGroupId}");
        //     
        //     await _cacheTrackingAction.Save(cacheKey, trackingActionEntity, null, synchronizationLock);
        // }
        
        var semaphore = new SemaphoreSlim(20); // Limite à 20 tâches en parallèle
        var tasks = actionsGroupDefinitions.Select(async groupDefinition =>
        {
            await semaphore.WaitAsync();
            try
            {
                var trackingActionEntity = new TrackingActionEntity
                {
                    ActionsGroupId = groupDefinition.ActionsGroupId,
                    SourceClientInstanceId = groupDefinition.Source,
                    TargetClientInstanceIds = [..groupDefinition.Targets],
                    Size = groupDefinition.Size,
                };
        
                var cacheKey = _cacheService.ComputeCacheKey(EntityType.TrackingAction, $"{synchronizationEntity.SessionId}_{groupDefinition.ActionsGroupId}");
        
                await _cacheTrackingAction.Save(cacheKey, trackingActionEntity, null, synchronizationLock);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        
        // await _actionsGroupDefinitionsRepository.AddOrUpdateActionsGroupDefinitions(synchronizationEntity.SessionId, actionsGroupDefinitions);
    }

    public async Task ResetSession(string sessionId)
    {
        await Delete(sessionId);
        
        // await _actionsGroupDefinitionsRepository.DeleteActionsGroupDefinitions(sessionId);
    }
}