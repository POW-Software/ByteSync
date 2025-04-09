using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IRepository<T>
{
    // string ComputeCacheKey(params string[] keyParts);
    
    EntityType EntityType { get; }
    
    Task<T?> Get(string key);
    
    Task<T?> Get(string key, ITransaction? transaction);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler, ITransaction? transaction);
    
    Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler, ITransaction? transaction = null, IRedLock? redisLock = null);
    
    Task<UpdateEntityResult<T>> UpdateIfExists(string key, Func<T, bool> updateHandler, ITransaction? transaction = null, IRedLock? redisLock = null);
    
    Task<UpdateEntityResult<T>> Save(CacheKey cacheKey, T element, ITransaction? transaction = null);
    
    Task<UpdateEntityResult<T>> Save(string key, T element, ITransaction? transaction = null);

    Task<UpdateEntityResult<T>> SetElement(CacheKey cacheKey, T createdOrUpdatedElement, IDatabaseAsync database);

    Task Delete(string key);
    
    Task Delete(string key, ITransaction? transaction);
}