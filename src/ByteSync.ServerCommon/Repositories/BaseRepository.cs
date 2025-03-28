﻿using System.Text;
using ByteSync.Common.Controls.Json;
using ByteSync.Common.Helpers;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using RedLockNet;
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

    public async Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler, ITransaction? transaction = null,
        IRedLock? redisLock = null)
    {
        var updateEntityResult = await DoUpdate(key, updateHandler, true, transaction, redisLock);

        return updateEntityResult;
    }
    
    public async Task<UpdateEntityResult<T>> UpdateIfExists(string key, Func<T, bool> updateHandler, ITransaction? transaction = null,
        IRedLock? redisLock = null)
    {
        var updateEntityResult = await DoUpdate(key, updateHandler, false, transaction, redisLock);

        return updateEntityResult;
    }

    private async Task<UpdateEntityResult<T>> DoUpdate(string key, Func<T, bool> updateHandler, bool throwIfNotExists, ITransaction? transaction,
        IRedLock? redisLockParam)
    {
        var cacheKey = ComputeCacheKey(ElementName, key);
        IDatabaseAsync database = _cacheService.GetDatabase(transaction);
        
        IRedLock? redisLock = redisLockParam;
        bool shouldDispose = false;
        
        if (redisLock == null)
        {
            redisLock = await _cacheService.RedLockFactory.CreateLockAsync(cacheKey, TimeSpan.FromSeconds(30));
            shouldDispose = true;
        }

        try
        {
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
                throw new AcquireRedisLockException(key, redisLock);
            }
        }
        finally
        {
            if (shouldDispose && redisLock != null)
            {
                await redisLock.DisposeAsync();
            }
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
            cachedElement = JsonHelper.Deserialize<T>(serializedElement!);
        }

        return cachedElement;
    }
    
    public async Task<UpdateEntityResult<T>> SetElement(string cacheKey, T createdOrUpdatedElement, IDatabaseAsync database)
    {
        string serializedElement = JsonHelper.Serialize(createdOrUpdatedElement);

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