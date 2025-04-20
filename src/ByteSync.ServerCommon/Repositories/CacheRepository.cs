using ByteSync.Common.Controls.Json;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public class CacheRepository<T> : ICacheRepository<T> where T : class
{
    private readonly IRedisInfrastructureService _redisInfrastructureService;
    private readonly TimeSpan _expiry = TimeSpan.FromDays(2);

    public CacheRepository(IRedisInfrastructureService redisInfrastructureService)
    {
        _redisInfrastructureService = redisInfrastructureService;
    }

    public async Task<T?> Get(CacheKey cacheKey)
    {
        IDatabaseAsync database = _redisInfrastructureService.GetDatabase();
        string? serializedElement = await database.StringGetAsync(cacheKey.Value);

        if (serializedElement.IsNullOrEmpty())
        {
            return null;
        }
            
        return JsonHelper.Deserialize<T>(serializedElement!);
    }

    public async Task<UpdateEntityResult<T>> Save(CacheKey cacheKey, T element, ITransaction? transaction = null, IRedLock? redisLock = null)
    {
        IDatabaseAsync database = _redisInfrastructureService.GetDatabase(transaction);
        bool shouldDispose = redisLock == null;
        redisLock ??= await _redisInfrastructureService.AcquireLockAsync(cacheKey);

        try
        {
            return await SaveInternal(cacheKey, element, database);
        }
        finally
        {
            if (shouldDispose)
            {
                await redisLock.DisposeAsync();
            }
        }
    }

    public async Task<UpdateEntityResult<T>> Update(CacheKey cacheKey, Func<T, bool> updateHandler, bool throwIfNotExists,
        ITransaction? transaction = null, IRedLock? redisLock = null)
    {
        IDatabaseAsync database = _redisInfrastructureService.GetDatabase(transaction);
        bool shouldDispose = redisLock == null;
        redisLock ??= await _redisInfrastructureService.AcquireLockAsync(cacheKey);
        
        try
        {
            var cachedElement = await Get(cacheKey);
            
            if (cachedElement == null)
            {
                if (throwIfNotExists)
                {
                    throw new Exception("Could not find element to update");
                }
                return new UpdateEntityResult<T>(cachedElement, UpdateEntityStatus.NotFound);
            }
            
            bool isUpdateDone = updateHandler.Invoke(cachedElement);
            if (!isUpdateDone)
            {
                return new UpdateEntityResult<T>(cachedElement, UpdateEntityStatus.NoOperation);
            }
                
            return await SaveInternal(cacheKey, cachedElement, database);
        }
        finally
        {
            if (shouldDispose)
            {
                await redisLock.DisposeAsync();
            }
        }
    }

    public async Task<UpdateEntityResult<T>> AddOrUpdate(CacheKey cacheKey, Func<T?, T?> handler, ITransaction? transaction = null, IRedLock? redisLock = null)
    {
        IDatabaseAsync database = _redisInfrastructureService.GetDatabase(transaction);
        bool shouldDispose = redisLock == null;
        redisLock ??= await _redisInfrastructureService.AcquireLockAsync(cacheKey);
        
        try
        {
            var cachedElement = await Get(cacheKey);
            var createdOrUpdatedElement = handler.Invoke(cachedElement);

            if (createdOrUpdatedElement == null)
            {
                return new UpdateEntityResult<T>(cachedElement, UpdateEntityStatus.NoOperation);
            }
                
            return await SaveInternal(cacheKey, createdOrUpdatedElement, database);
        }
        finally
        {
            if (shouldDispose)
            {
                await redisLock.DisposeAsync();
            }
        }
    }

    public async Task Delete(CacheKey cacheKey, ITransaction? transaction = null)
    {
        IDatabaseAsync database = _redisInfrastructureService.GetDatabase(transaction);
        await database.KeyDeleteAsync(cacheKey.Value);
    }

    public async Task<IRedLock> AcquireLockAsync(CacheKey cacheKey)
    {
        return await _redisInfrastructureService.AcquireLockAsync(cacheKey);
    }
    
    private async Task<UpdateEntityResult<T>> SaveInternal(CacheKey cacheKey, T element, IDatabaseAsync database)
    {
        string serializedElement = JsonHelper.Serialize(element);

        if (database is ITransaction)
        {
            _ = database.StringSetAsync(cacheKey.Value, serializedElement, _expiry);
            return new UpdateEntityResult<T>(element, UpdateEntityStatus.WaitingForTransaction);
        }
        
        await database.StringSetAsync(cacheKey.Value, serializedElement, _expiry);
        return new UpdateEntityResult<T>(element, UpdateEntityStatus.Saved);
    }
}