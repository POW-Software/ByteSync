using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Repositories;

public class TrackingActionRepository : BaseRepository<TrackingActionEntity>, ITrackingActionRepository
{
    private readonly IRedisInfrastructureService _redisInfrastructureService;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ITrackingActionEntityFactory _trackingActionEntityFactory;
    private readonly ILogger<TrackingActionRepository> _logger;
    
    public TrackingActionRepository(IRedisInfrastructureService redisInfrastructureService, ISynchronizationRepository synchronizationRepository, 
        ITrackingActionEntityFactory trackingActionEntityFactory, ICacheRepository<TrackingActionEntity> cacheRepository,
        ILogger<TrackingActionRepository> logger) : base(redisInfrastructureService, cacheRepository)
    {
        _redisInfrastructureService = redisInfrastructureService;
        _synchronizationRepository = synchronizationRepository;
        _trackingActionEntityFactory = trackingActionEntityFactory;
        _logger = logger;
    }

    public override EntityType EntityType => EntityType.TrackingAction;
    
    public async Task<TrackingActionEntity> GetOrBuild(string sessionId, string actionsGroupId)
    {
        var cacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType, $"{sessionId}_{actionsGroupId}");
        
        await using var actionsGroupIdLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey);

        return await DoGetOrBuild(sessionId, actionsGroupId, cacheKey);
    }

    private async Task<TrackingActionEntity> DoGetOrBuild(string sessionId, string actionsGroupId, CacheKey cacheKey)
    {
        var trackingActionEntity = await Get($"{sessionId}_{actionsGroupId}");
        
        if (trackingActionEntity == null)
        {
            trackingActionEntity = await _trackingActionEntityFactory.Create(sessionId, actionsGroupId);
            
            await Save(cacheKey, trackingActionEntity);
        }
        
        return trackingActionEntity;
    }

    public async Task<TrackingActionResult> AddOrUpdate(string sessionId, List<string> actionsGroupIds, 
        Func<TrackingActionEntity, SynchronizationEntity, bool> updateHandler)
    {
        var synchronizationCacheKey = _redisInfrastructureService.ComputeCacheKey(EntityType.Synchronization, sessionId);
        await using var synchronizationLock = await _redisInfrastructureService.AcquireLockAsync(synchronizationCacheKey);
        
        var synchronizationEntity = (await _synchronizationRepository.Get(sessionId))!;
        var transaction = _redisInfrastructureService.OpenTransaction();

        var locks = new List<IAsyncDisposable>();
        
        List<TrackingActionEntity> trackingActionEntities = new List<TrackingActionEntity>();
        bool areAllUpdated = true;
        foreach (var actionsGroupId in actionsGroupIds)
        {
            if (!areAllUpdated)
            {
                break;
            }
            
            var cacheKey  = _redisInfrastructureService.ComputeCacheKey(EntityType, $"{sessionId}_{actionsGroupId}");
            var actionsGroupIdLock = await _redisInfrastructureService.AcquireLockAsync(cacheKey); 
            locks.Add(actionsGroupIdLock);
            
            var trackingActionEntity = await DoGetOrBuild(sessionId, actionsGroupId, cacheKey);
            bool isUpdated = updateHandler.Invoke(trackingActionEntity, synchronizationEntity);

            if (isUpdated)
            {
                await Save(cacheKey, trackingActionEntity, transaction);
                trackingActionEntities.Add(trackingActionEntity);
            }
            else
            {
                _logger.LogWarning("AddOrUpdate: can not update element {TrackingActionEntity} for session {SessionId}. No element will be updated",
                    trackingActionEntity.ActionsGroupId, sessionId);
            }

            areAllUpdated &= isUpdated;
        }

        if (areAllUpdated)
        {
            await _synchronizationRepository.Save(synchronizationCacheKey, synchronizationEntity, transaction);
            
            await transaction.ExecuteAsync();
        }

        foreach (var redisLock in locks)
        {
            await redisLock.DisposeAsync();
        }
        
        TrackingActionResult result = new TrackingActionResult(areAllUpdated, trackingActionEntities, synchronizationEntity);
        
        return result;
    }
}