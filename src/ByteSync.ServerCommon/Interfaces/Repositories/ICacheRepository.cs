using ByteSync.ServerCommon.Business.Repositories;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface ICacheRepository<T> where T : class
{
    Task<T?> Get(CacheKey cacheKey);
    
    Task<UpdateEntityResult<T>> Save(CacheKey cacheKey, T element, ITransaction? transaction = null, IRedLock? redisLock = null);
    
    Task<UpdateEntityResult<T>> Update(CacheKey cacheKey, Func<T, bool> updateHandler, bool throwIfNotExists, ITransaction? transaction = null, 
        IRedLock? redisLock = null);
    
    Task<UpdateEntityResult<T>> AddOrUpdate(CacheKey cacheKey, Func<T?, T?> handler, ITransaction? transaction = null, IRedLock? redisLock = null);
    
    Task Delete(CacheKey cacheKey, ITransaction? transaction = null);
    
    // Task<IRedLock> AcquireLockAsync(CacheKey cacheKey);
}