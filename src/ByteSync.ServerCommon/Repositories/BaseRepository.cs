using System.Text;
using System.Text.Json;
using ByteSync.Common.Controls.Json;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly ICacheService _cacheService;

    protected BaseRepository(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }
    
    private string Prefix => _cacheService.Prefix;
    
    public abstract string ElementName { get; }
    
    private TimeSpan Expiry => TimeSpan.FromDays(2);
    
    public string ComputeCacheKey(params string[] keyParts)
    {
        StringBuilder sb = new StringBuilder(Prefix);

        foreach (var keyPart in keyParts)
        {
            sb.Append($":{keyPart}");
        }
        
        return sb.ToString();
    }
    
    public Task<T?> Get(string key)
    {
        return Get(key, null);
    }
    
    public async Task<T?> Get(string key, ITransaction? transaction)
    {
        var cacheKey = ComputeCacheKey(ElementName, key);
        
        var cachedElement = await GetCachedElement(cacheKey);
        return cachedElement;
    }

    public async Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler)
    {
        return await AddOrUpdate(key, handler, null);
    }

    public async Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler, ITransaction? transaction)
    {
        var cacheKey = ComputeCacheKey(ElementName, key);
        IDatabaseAsync database = _cacheService.GetDatabase(transaction);
        await using var redisLock = await _cacheService.AcquireLockAsync(cacheKey); 
        
        var cachedElement = await GetCachedElement(cacheKey);
        var createdOrUpdatedElement = handler.Invoke(cachedElement);
                    
        if (createdOrUpdatedElement == null)
        {
            return new UpdateEntityResult<T>(cachedElement, UpdateEntityStatus.NoOperation);
        }
        else
        {
            return await SetElement(cacheKey, createdOrUpdatedElement, database);
        }
    }

    public async Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler)
    {
        var updateEntityResult = await DoUpdate(key, updateHandler, true, null);

        return updateEntityResult;
    }

    public async Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler, ITransaction transaction)
    {
        var updateEntityResult = await DoUpdate(key, updateHandler, true, transaction);

        return updateEntityResult;
    }
    
    public async Task<UpdateEntityResult<T>> UpdateIfExists(string key, Func<T, bool> updateHandler, ITransaction? transaction = null)
    {
        var updateEntityResult = await DoUpdate(key, updateHandler, false, transaction);

        return updateEntityResult;
    }

    private async Task<UpdateEntityResult<T>> DoUpdate(string key, Func<T, bool> updateHandler, bool throwIfNotExists, ITransaction? transaction)
    {
        var cacheKey = ComputeCacheKey(ElementName, key);
        IDatabaseAsync database = _cacheService.GetDatabase(transaction);
        
        await using var redisLock = await _cacheService.RedLockFactory.CreateLockAsync(cacheKey, TimeSpan.FromSeconds(30));
        
        if (redisLock.IsAcquired)
        {
            var cachedElement = await GetCachedElement(cacheKey);
    
            if (cachedElement == null)
            {
                if (throwIfNotExists)
                {
                    throw new Exception("Could not find element to update");
                }
                else
                {
                    return new UpdateEntityResult<T>(cachedElement, UpdateEntityStatus.NotFound);
                }
            }
    
            bool isUpdateDone = updateHandler.Invoke(cachedElement);
            if (!isUpdateDone)
            {
                return new UpdateEntityResult<T>(cachedElement, UpdateEntityStatus.NoOperation);
            }
            else
            {
                return await SetElement(cacheKey, cachedElement, database);
            }
        }
        else
        {
            throw new Exception("Could not acquire redis lock");
        }
    }
    
    public async Task<UpdateEntityResult<T>> Save(string key, T element, ITransaction? transaction = null)
    {
        var cacheKey = ComputeCacheKey(ElementName, key);
        IDatabaseAsync database = _cacheService.GetDatabase(transaction);
        
        await using var redisLock = await _cacheService.AcquireLockAsync(cacheKey); 
        return await SetElement(cacheKey, element, database);
    }

    protected async Task<T?> GetCachedElement(string cacheKey)
    {
        T? cachedElement = default(T);
                
        string? serializedElement = await _cacheService.GetDatabase().StringGetAsync(cacheKey);
        if (serializedElement.IsNotEmpty())
        {
            var settings = JsonSerializerOptionsHelper.BuildOptions(true, true);
            cachedElement = JsonSerializer.Deserialize<T>(serializedElement!, settings);
        }

        return cachedElement;
    }
    
    public async Task<UpdateEntityResult<T>> SetElement(string cacheKey, T createdOrUpdatedElement, IDatabaseAsync database)
    {
        // https://stackoverflow.com/questions/13510204/json-net-self-referencing-loop-detected
        var settings = JsonSerializerOptionsHelper.BuildOptions(true, true);
        
        string serializedElement = JsonSerializer.Serialize(createdOrUpdatedElement, settings);

        if (database is ITransaction)
        {
            _ = database.StringSetAsync(cacheKey, serializedElement, Expiry);
            
            return new UpdateEntityResult<T>(createdOrUpdatedElement, UpdateEntityStatus.WaitingForTransaction);
        }
        else
        {
            await database.StringSetAsync(cacheKey, serializedElement, Expiry);
            
            return new UpdateEntityResult<T>(createdOrUpdatedElement, UpdateEntityStatus.Saved);
        }
    }
    
    public async Task Delete(string key)
    {
        await Delete(key, null);
    }
    
    public async Task Delete(string key, ITransaction? transaction)
    {
        var cacheKey = ComputeCacheKey(ElementName, key);
        IDatabaseAsync database = _cacheService.GetDatabase(transaction);
        
        await database.KeyDeleteAsync(cacheKey);
    }
}