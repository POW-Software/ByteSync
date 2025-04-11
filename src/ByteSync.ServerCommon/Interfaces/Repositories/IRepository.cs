using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IRepository<T>
{
    EntityType EntityType { get; }
    
    Task<T?> Get(string key);
    
    Task<T?> Get(CacheKey cacheKey);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler, ITransaction? transaction);
    
    Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler, ITransaction? transaction = null, IRedLock? redisLock = null);
    
    Task<UpdateEntityResult<T>> UpdateIfExists(string key, Func<T, bool> updateHandler, ITransaction? transaction = null, IRedLock? redisLock = null);
    
    Task<UpdateEntityResult<T>> Save(string key, T element, ITransaction? transaction = null, IRedLock? redisLock = null);

    Task Delete(string key);
    
    Task Delete(string key, ITransaction? transaction);
}