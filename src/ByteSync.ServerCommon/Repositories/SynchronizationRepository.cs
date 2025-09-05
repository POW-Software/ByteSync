using ByteSync.Common.Business.Actions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class SynchronizationRepository : BaseRepository<SynchronizationEntity>, ISynchronizationRepository
{
    private readonly ICacheRepository<TrackingActionEntity> _cacheTrackingAction;
    
    public SynchronizationRepository(IRedisInfrastructureService redisInfrastructureService, ICacheRepository<SynchronizationEntity> cacheRepository,
        ICacheRepository<TrackingActionEntity> cacheTrackingAction) : base(redisInfrastructureService, cacheRepository)
    {
        _cacheTrackingAction = cacheTrackingAction;
    }

    public override EntityType EntityType => EntityType.Synchronization;

    public async Task AddSynchronization(SynchronizationEntity synchronizationEntity, List<ActionsGroupDefinition> actionsGroupDefinitions)
    {
        var synchronizationCacheKey = _cacheService.ComputeCacheKey(EntityType.Synchronization, synchronizationEntity.SessionId);
        await using var synchronizationLock = await _cacheService.AcquireLockAsync(synchronizationCacheKey);
        
        await Save(synchronizationEntity.SessionId, synchronizationEntity, null, synchronizationLock);
        
        var semaphore = new SemaphoreSlim(20); // Limit to 20 tasks in parallel
        var tasks = actionsGroupDefinitions.Select(async groupDefinition =>
        {
            await semaphore.WaitAsync();
            try
            {
                var trackingActionEntity = new TrackingActionEntity
                {
                    ActionsGroupId = groupDefinition.ActionsGroupId,
                    SourceClientInstanceId = groupDefinition.IsInitialOperatingOnSourceNeeded 
                        ? groupDefinition.SourceClientInstanceId 
                        : null,
                    TargetClientInstanceAndNodeIds = [..groupDefinition.TargetClientInstanceAndNodeIds],
                    Size = groupDefinition.Size,
                };
        
                var cacheKey = _cacheService.ComputeCacheKey(EntityType.TrackingAction, $"{synchronizationEntity.SessionId}_{groupDefinition.ActionsGroupId}");
        
                // ReSharper disable once AccessToDisposedClosure
                await _cacheTrackingAction.Save(cacheKey, trackingActionEntity, null, synchronizationLock);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task ResetSession(string sessionId)
    {
        await Delete(sessionId);
    }
}