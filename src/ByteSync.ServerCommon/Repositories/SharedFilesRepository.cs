using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class SharedFilesRepository : BaseRepository<SharedFileData>, ISharedFilesRepository
{
    private readonly IRedisInfrastructureService _redisInfrastructureService;

    public SharedFilesRepository(IRedisInfrastructureService redisInfrastructureService, 
        ICacheRepository<SharedFileData> cacheRepository) : base(redisInfrastructureService, cacheRepository)
    {
        _redisInfrastructureService = redisInfrastructureService;
    }
    
    private CacheKey ComputeSharedFileCacheKey(SharedFileDefinition sharedFileDefinition)
    {
        return ComputeSharedFileCacheKey(sharedFileDefinition.Id);
    }
    
    private CacheKey ComputeSharedFileCacheKey(string sharedFileDefinitionId)
    {
        return _redisInfrastructureService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinitionId);
    }
    
    public override EntityType EntityType { get; } = EntityType.SharedFile;

    private CacheKey ComputeSessionSharedFilesKey(SharedFileDefinition sharedFileDefinition)
    {
        return ComputeSessionSharedFilesKey(sharedFileDefinition.SessionId);
    }
    
    private CacheKey ComputeSessionSharedFilesKey(string sessionId)
    {
        return _redisInfrastructureService.ComputeCacheKey(EntityType.SessionSharedFiles, sessionId);
    }
    
    public async Task AddOrUpdate(SharedFileDefinition sharedFileDefinition, Func<SharedFileData?, SharedFileData> updateHandler)
    {
        var sessionSharedFilesKey = ComputeSessionSharedFilesKey(sharedFileDefinition);
        var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinition);
        
        var transaction = _redisInfrastructureService.OpenTransaction();
        // var transaction = _redisInfrastructureService.GetDatabase();
        
        await using var sessionSharedFilesLock = await _redisInfrastructureService.AcquireLockAsync(sessionSharedFilesKey); 
        await using var sharedFileLock = await _redisInfrastructureService.AcquireLockAsync(sharedFileCacheKey);

        var cachedElement = await Get(sharedFileCacheKey); 
        var element = updateHandler.Invoke(cachedElement);
        await Save(sharedFileCacheKey, element, transaction, sessionSharedFilesLock);
        // await Save(sharedFileCacheKey, element, null, sessionSharedFilesLock);
        _ = transaction.SetAddAsync(sessionSharedFilesKey.Value, sharedFileDefinition.Id);
        
        await transaction.ExecuteAsync();
    }

    public async Task Forget(SharedFileDefinition sharedFileDefinition)
    {
        var sessionSharedFilesKey = ComputeSessionSharedFilesKey(sharedFileDefinition);
        var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinition);

        var transaction = _redisInfrastructureService.OpenTransaction();
        
        await using var sessionSharedFilesLock = await _redisInfrastructureService.AcquireLockAsync(sessionSharedFilesKey); 
        await using var sharedFileLock = await _redisInfrastructureService.AcquireLockAsync(sharedFileCacheKey);

        _ = transaction.KeyDeleteAsync(sharedFileCacheKey.Value);
        _ = transaction.SetRemoveAsync(sessionSharedFilesKey.Value, sharedFileDefinition.Id);
        
        await transaction.ExecuteAsync();
    }
    
    public async Task<List<SharedFileData>> Clear(string sessionId)
    {
        List<SharedFileData> result = new List<SharedFileData>();
        
        var sessionSharedFilesKey = ComputeSessionSharedFilesKey(sessionId);
        
        await using var sessionSharedFilesLock = await _redisInfrastructureService.AcquireLockAsync(sessionSharedFilesKey);

        var database = _redisInfrastructureService.GetDatabase();
        var redisValues = await database.SetMembersAsync(sessionSharedFilesKey.Value);
        List<string> sharedFileDefinitionIds = redisValues.Select(value => value.ToString()).ToList();

        var transaction = _redisInfrastructureService.OpenTransaction();
        foreach (var sharedFileDefinitionId in sharedFileDefinitionIds)
        {
            var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinitionId);
            await using var sharedFileLock = await _redisInfrastructureService.AcquireLockAsync(sharedFileCacheKey);

            var sharedFileData = await Get(sharedFileCacheKey);
                    
            if (sharedFileData != null)
            {
                result.Add(sharedFileData);
                        
                _ = transaction.KeyDeleteAsync(sharedFileCacheKey.Value);
            }
        }
            
        _ = transaction.KeyDeleteAsync(sessionSharedFilesKey.Value);
        
        await transaction.ExecuteAsync();

        return result;
    }
}