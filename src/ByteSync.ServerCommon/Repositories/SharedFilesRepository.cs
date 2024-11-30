using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Sessions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class SharedFilesRepository : BaseRepository<SharedFileData>, ISharedFilesRepository
{
    public SharedFilesRepository(ICacheService cacheService) : base(cacheService)
    {
    }
    
    private string ComputeSharedFileCacheKey(SharedFileDefinition sharedFileDefinition)
    {
        return ComputeSharedFileCacheKey(sharedFileDefinition.Id);
    }
    
    private string ComputeSharedFileCacheKey(string sharedFileDefinitionId)
    {
        return ComputeCacheKey("SharedFile", sharedFileDefinitionId);
    }
    
    public override string ElementName { get; } = "SharedFile";

    private string ComputeSessionSharedFilesKey(SharedFileDefinition sharedFileDefinition)
    {
        return ComputeSessionSharedFilesKey(sharedFileDefinition.SessionId);
    }
    
    private string ComputeSessionSharedFilesKey(string sessionId)
    {
        return ComputeCacheKey("SessionSharedFiles", sessionId);
    }
    
    public async Task AddOrUpdate(SharedFileDefinition sharedFileDefinition, Func<SharedFileData?, SharedFileData> updateHandler)
    {
        string sessionSharedFilesKey = ComputeSessionSharedFilesKey(sharedFileDefinition);
        string sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinition);

        var database = _cacheService.GetDatabase();
        
        await using var sessionSharedFilesLock = await _cacheService.AcquireLockAsync(sessionSharedFilesKey); 
        await using var sharedFileLock = await _cacheService.AcquireLockAsync(sharedFileCacheKey);

        var cachedElement = await GetCachedElement(sharedFileCacheKey); 
        var element = updateHandler.Invoke(cachedElement);
        await SetElement(sharedFileCacheKey, element, database);
        await database.SetAddAsync(sessionSharedFilesKey, sharedFileDefinition.Id);
    }

    public async Task Forget(SharedFileDefinition sharedFileDefinition)
    {
        string sessionSharedFilesKey = ComputeSessionSharedFilesKey(sharedFileDefinition);
        string sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinition);

        var database = _cacheService.GetDatabase();
        
        
        await using var sessionSharedFilesLock = await _cacheService.AcquireLockAsync(sessionSharedFilesKey); 
        await using var sharedFileLock = await _cacheService.AcquireLockAsync(sharedFileCacheKey);

        await database.KeyDeleteAsync(sharedFileCacheKey);
        await database.SetRemoveAsync(sessionSharedFilesKey, sharedFileDefinition.Id);
    }
    
    public async Task<List<SharedFileData>> Clear(string sessionId)
    {
        List<SharedFileData> result = new List<SharedFileData>();
        
        string sessionSharedFilesKey = ComputeSessionSharedFilesKey(sessionId);

        var database = _cacheService.GetDatabase();
        
        await using var sessionSharedFilesLock = await _cacheService.RedLockFactory.CreateLockAsync(sessionSharedFilesKey, TimeSpan.FromSeconds(30));

        if (sessionSharedFilesLock.IsAcquired)
        {
            var redisValues = await database.SetMembersAsync(sessionSharedFilesKey);
            List<string> sharedFileDefinitionIds = redisValues.Select(value => value.ToString()).ToList();

            foreach (var sharedFileDefinitionId in sharedFileDefinitionIds)
            {
                var sharedFileCacheKey = ComputeSharedFileCacheKey(sharedFileDefinitionId);
                await using var sharedFileLock = await _cacheService.RedLockFactory.CreateLockAsync(sharedFileCacheKey, TimeSpan.FromSeconds(30));

                if (sharedFileLock.IsAcquired)
                {
                    var sharedFileData = await GetCachedElement(sharedFileCacheKey);
                    
                    if (sharedFileData != null)
                    {
                        result.Add(sharedFileData);
                        
                        await database.KeyDeleteAsync(sharedFileCacheKey);
                    }
                }
                else
                {
                    throw new Exception("Could not acquire redis lock");
                }
            }
            
            await database.KeyDeleteAsync(sessionSharedFilesKey);
        }
        else
        {
            throw new Exception("Could not acquire redis lock");
        }

        return result;
    }
}