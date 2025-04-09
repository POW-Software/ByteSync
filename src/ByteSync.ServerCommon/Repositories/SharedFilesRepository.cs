using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class SharedFilesRepository : BaseRepository<SharedFileData>, ISharedFilesRepository
{
    public SharedFilesRepository(ICacheService cacheService) : base(cacheService)
    {
    }
    
    private CacheKey ComputeSharedFileCacheKey(SharedFileDefinition sharedFileDefinition)
    {
        return ComputeSharedFileCacheKey(sharedFileDefinition.Id);
    }
    
    private CacheKey ComputeSharedFileCacheKey(string sharedFileDefinitionId)
    {
        return _cacheService.ComputeCacheKey(EntityType.SharedFile, sharedFileDefinitionId);
    }
    
    public override EntityType EntityType { get; } = EntityType.SharedFile;

    private CacheKey ComputeSessionSharedFilesKey(SharedFileDefinition sharedFileDefinition)
    {
        return ComputeSessionSharedFilesKey(sharedFileDefinition.SessionId);
    }
    
    private CacheKey ComputeSessionSharedFilesKey(string sessionId)
    {
        return _cacheService.ComputeCacheKey(EntityType.SessionSharedFiles, sessionId);
    }
    
    public async Task AddOrUpdate(SharedFileDefinition sharedFileDefinition, Func<SharedFileData?, SharedFileData> updateHandler)
    {
        var sessionSharedFilesKey = ComputeSessionSharedFilesKey(sharedFileDefinition);
        var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinition);

        var database = _cacheService.GetDatabase();
        
        await using var sessionSharedFilesLock = await _cacheService.AcquireLockAsync(sessionSharedFilesKey); 
        await using var sharedFileLock = await _cacheService.AcquireLockAsync(sharedFileCacheKey);

        var cachedElement = await GetCachedElement(sharedFileCacheKey); 
        var element = updateHandler.Invoke(cachedElement);
        await SetElement(sharedFileCacheKey, element, database);
        await database.SetAddAsync(sessionSharedFilesKey.Value, sharedFileDefinition.Id);
    }

    public async Task Forget(SharedFileDefinition sharedFileDefinition)
    {
        var sessionSharedFilesKey = ComputeSessionSharedFilesKey(sharedFileDefinition);
        var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinition);

        var database = _cacheService.GetDatabase();
        
        
        await using var sessionSharedFilesLock = await _cacheService.AcquireLockAsync(sessionSharedFilesKey); 
        await using var sharedFileLock = await _cacheService.AcquireLockAsync(sharedFileCacheKey);

        await database.KeyDeleteAsync(sharedFileCacheKey.Value);
        await database.SetRemoveAsync(sessionSharedFilesKey.Value, sharedFileDefinition.Id);
    }
    
    public async Task<List<SharedFileData>> Clear(string sessionId)
    {
        List<SharedFileData> result = new List<SharedFileData>();
        
        var sessionSharedFilesKey = ComputeSessionSharedFilesKey(sessionId);

        var database = _cacheService.GetDatabase();
        
        await using var sessionSharedFilesLock = await _cacheService.AcquireLockAsync(sessionSharedFilesKey);

        var redisValues = await database.SetMembersAsync(sessionSharedFilesKey.Value);
        List<string> sharedFileDefinitionIds = redisValues.Select(value => value.ToString()).ToList();

        foreach (var sharedFileDefinitionId in sharedFileDefinitionIds)
        {
            var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinitionId);
            await using var sharedFileLock = await _cacheService.AcquireLockAsync(sharedFileCacheKey);

            var sharedFileData = await GetCachedElement(sharedFileCacheKey);
                    
            if (sharedFileData != null)
            {
                result.Add(sharedFileData);
                        
                await database.KeyDeleteAsync(sharedFileCacheKey.Value);
            }
        }
            
        await database.KeyDeleteAsync(sessionSharedFilesKey.Value);

        return result;
    }
}