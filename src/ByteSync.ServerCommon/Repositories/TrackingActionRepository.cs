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
    private readonly ITrackingActionEntityFactory _trackingActionEntityFactory;
    private readonly ICacheRepository<SynchronizationEntity> _synchronizationCacheRepository;
    private readonly ILogger<TrackingActionRepository> _logger;

    public TrackingActionRepository(IRedisInfrastructureService redisInfrastructureService, ISynchronizationRepository synchronizationRepository,
        ITrackingActionEntityFactory trackingActionEntityFactory, ICacheRepository<TrackingActionEntity> cacheRepository,
        ICacheRepository<SynchronizationEntity> synchronizationCacheRepository, ILogger<TrackingActionRepository> logger)
        : base(redisInfrastructureService, cacheRepository)
    {
        _redisInfrastructureService = redisInfrastructureService;
        _synchronizationRepository = synchronizationRepository;
        _trackingActionEntityFactory = trackingActionEntityFactory;
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
            trackingActionEntity = await _trackingActionEntityFactory.Create(sessionId, actionsGroupId);

            await Save(cacheKey, trackingActionEntity, null, actionsGroupIdLock);
        }

        return trackingActionEntity;
    }

    public async Task<TrackingActionResult> AddOrUpdate(string sessionId, List<string> actionsGroupIds,
        Func<TrackingActionEntity, SynchronizationEntity, TrackingActionUpdateHandlerResult> updateHandler)
    {
        // CacheKey? synchronizationCacheKey = null;
        // IRedLock? synchronizationLock = null;

        var locks = new ConcurrentBag<IAsyncDisposable>(); // Thread-safe
        var trackingActionEntities = new ConcurrentBag<TrackingActionEntity>();
        var updateHandlerResults = new List<TrackingActionUpdateHandlerResult>();
        // bool areAllUpdated = true;

        // if (updateSynchronization)
        // {
        //     synchronizationCacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        //     synchronizationLock = await _redisInfrastructureService.AcquireLockAsync(synchronizationCacheKey);
        //     locks.Add(synchronizationLock);
        // }

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
                var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType, $"{sessionId}_{actionsGroupId}");
                var actionsGroupIdLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey);
                locks.Add(actionsGroupIdLock);

                var trackingActionEntity = await DoGetOrBuild(sessionId, actionsGroupId, cacheKey, actionsGroupIdLock);
                var updateHandlerResult = updateHandler.Invoke(trackingActionEntity, synchronizationEntity);

                if (updateHandlerResult.IsSuccess)
                {
                    await Save(cacheKey, trackingActionEntity, transaction, actionsGroupIdLock);
                    trackingActionEntities.Add(trackingActionEntity);
                    updateHandlerResults.Add(updateHandlerResult);
                }
                else
                {
                    _logger.LogWarning("AddOrUpdate: can not update element {TrackingActionEntity} for session {SessionId}. No element will be updated",
                        trackingActionEntity.ActionsGroupId, sessionId);

                    updateFailures.Add(true); // flag pour arrêt
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
            if (updateHandlerResults.Any(uhr => uhr.IsAChange))
            {
                var synchronizationCacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
                await _synchronizationCacheRepository.AddOrUpdate(synchronizationCacheKey, synEnt =>
                {
                    foreach (var updateHandlerResult in updateHandlerResults)
                    {
                        synEnt!.Progress.FinishedActionsCount += updateHandlerResult.FinishedActionsCount;
                        synEnt.Progress.ErrorsCount += updateHandlerResult.ErrorsCount;
                        synEnt.Progress.ProcessedVolume += updateHandlerResult.ProcessedVolume;
                        synEnt.Progress.ExchangedVolume += updateHandlerResult.ExchangedVolume;
                    }

                    return synEnt;
                }, transaction);

                // await _synchronizationCacheRepository.Save(synchronizationCacheKey!, synchronizationEntity, transaction, synchronizationLock);
            }

            await transaction.ExecuteAsync();
        }

        foreach (var redisLock in locks)
        {
            await redisLock.DisposeAsync();
        }

        return new TrackingActionResult(areAllUpdated, trackingActionEntities.ToList(), synchronizationEntity);
    }
}