using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface IRedisInfrastructureService
{
    ITransaction OpenTransaction();
    
    IDatabaseAsync GetDatabase();
    
    IDatabaseAsync GetDatabase(ITransaction? transaction);

    Task<IRedLock> AcquireLockAsync(EntityType entityType, string entityId);
    
    Task<IRedLock> AcquireLockAsync(CacheKey cacheKey);
    
    CacheKey ComputeCacheKey(EntityType entityType, string s);
}