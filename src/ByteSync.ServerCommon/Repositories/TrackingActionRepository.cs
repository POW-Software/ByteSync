using System.Collections.Concurrent;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using RedLockNet;

namespace ByteSync.ServerCommon.Repositories;

public class TrackingActionRepository : BaseRepository<TrackingActionEntity>, ITrackingActionRepository
{
    private readonly IRedisInfrastructureService _redisInfrastructureService;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ICacheRepository<SynchronizationEntity> _synchronizationCacheRepository;
    private readonly ILogger<TrackingActionRepository> _logger;

    public TrackingActionRepository(IRedisInfrastructureService redisInfrastructureService, ISynchronizationRepository synchronizationRepository,
        ICacheRepository<TrackingActionEntity> cacheRepository,
        ICacheRepository<SynchronizationEntity> synchronizationCacheRepository, ILogger<TrackingActionRepository> logger)
        : base(redisInfrastructureService, cacheRepository)
    {
        _redisInfrastructureService = redisInfrastructureService;
        _synchronizationRepository = synchronizationRepository;
        _synchronizationCacheRepository = synchronizationCacheRepository;
        _logger = logger;
    }

    public override EntityType EntityType => EntityType.TrackingAction;

    public async Task<TrackingActionEntity> GetOrBuild(string sessionId, string actionsGroupId)
    {
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType, $"{sessionId}_{actionsGroupId}");

        await using var actionsGroupIdLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey);

        return await DoGetOrBuild(sessionId, actionsGroupId, cacheKey, actionsGroupIdLock);
    }

    private async Task<TrackingActionEntity> DoGetOrBuild(string sessionId, string actionsGroupId, CacheKey cacheKey, IRedLock actionsGroupIdLock)
    {
        var trackingActionEntity = await Get($"{sessionId}_{actionsGroupId}");

        if (trackingActionEntity == null)
        {
            throw new Exception("TrackingActionEntity is null");
        }

        return trackingActionEntity;
    }

    public async Task<TrackingActionResult> AddOrUpdate(string sessionId, List<string> actionsGroupIds,
        Func<TrackingActionEntity, SynchronizationEntity, bool> updateHandler)
    {
        var trackingActionEntities = new ConcurrentBag<TrackingActionEntity>();
        
        var synchronizationCacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        await using var synchronizationLock = await _redisInfrastructureService.AcquireLockAsync(synchronizationCacheKey);

        var synchronizationEntity = await _synchronizationRepository.Get(sessionId);
        if (synchronizationEntity == null)
        {
            throw new InvalidOperationException($"SynchronizationEntity for session {sessionId} not found");
        }

        var transaction = _redisInfrastructureService.OpenTransaction();

        var semaphore = new SemaphoreSlim(20);
        var updateFailures = new ConcurrentBag<bool>();

        var tasks = actionsGroupIds.Select(async actionsGroupId =>
        {
            await semaphore.WaitAsync();
            try
            {
                var cacheKey  = _redisInfrastructureService.ComputeCacheKey(EntityType, $"{sessionId}_{actionsGroupId}");
                var actionsGroupIdLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey); 
            
                var trackingActionEntity = await DoGetOrBuild(sessionId, actionsGroupId, cacheKey, actionsGroupIdLock);
                bool isUpdated = updateHandler.Invoke(trackingActionEntity, synchronizationEntity);

                if (isUpdated)
                {
                    await Save(cacheKey, trackingActionEntity, transaction, actionsGroupIdLock);
                    trackingActionEntities.Add(trackingActionEntity);
                }
                else
                {
                    _logger.LogWarning("AddOrUpdate: can not update element {TrackingActionEntity} for session {SessionId}. No element will be updated",
                        trackingActionEntity.ActionsGroupId, sessionId);

                    updateFailures.Add(true);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var areAllUpdated = updateFailures.IsEmpty;

        if (areAllUpdated)
        {
            await _synchronizationCacheRepository.Save(synchronizationCacheKey, synchronizationEntity, transaction, synchronizationLock);
            
            await transaction.ExecuteAsync();
        }

        return new TrackingActionResult(areAllUpdated, trackingActionEntities.ToList(), synchronizationEntity);
    }
}