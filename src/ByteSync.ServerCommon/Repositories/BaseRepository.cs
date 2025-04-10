using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using RedLockNet;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly IRedisInfrastructureService _cacheService;
    protected readonly ICacheRepository<T> _cacheRepository;

    protected BaseRepository(IRedisInfrastructureService cacheService, ICacheRepository<T> cacheRepository)
    {
        _cacheService = cacheService;
        _cacheRepository = cacheRepository;
    }
    
    public abstract EntityType EntityType { get; }
    
    public async Task<T?> Get(string key)
    {
        var cacheKey = _cacheService.ComputeCacheKey(EntityType, key);
        return await Get(cacheKey);
    }
    
    public async Task<T?> Get(CacheKey cacheKey)
    {
        return await _cacheRepository.Get(cacheKey);
    }

    public async Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler)
    {
        return await AddOrUpdate(key, handler, null);
    }

    public async Task<UpdateEntityResult<T>> AddOrUpdate(string key, Func<T?, T?> handler, ITransaction? transaction)
    {
        var cacheKey = _cacheService.ComputeCacheKey(EntityType, key);
        return await _cacheRepository.AddOrUpdate(cacheKey, handler, transaction);
    }

    public async Task<UpdateEntityResult<T>> Update(string key, Func<T, bool> updateHandler, ITransaction? transaction = null,
        IRedLock? redisLock = null)
    {
        var cacheKey = _cacheService.ComputeCacheKey(EntityType, key);
        return await _cacheRepository.Update(cacheKey, updateHandler, true, transaction, redisLock);
    }
    
    public async Task<UpdateEntityResult<T>> UpdateIfExists(string key, Func<T, bool> updateHandler, ITransaction? transaction = null,
        IRedLock? redisLock = null)
    {
        var cacheKey = _cacheService.ComputeCacheKey(EntityType, key);
        return await _cacheRepository.Update(cacheKey, updateHandler, false, transaction, redisLock);
    }

    public async Task<UpdateEntityResult<T>> Save(string key, T element, ITransaction? transaction = null, IRedLock? redisLock = null)
    {
        var cacheKey = _cacheService.ComputeCacheKey(EntityType, key);
        return await _cacheRepository.Save(cacheKey, element, transaction, redisLock);
    }
    
    public async Task<UpdateEntityResult<T>> Save(CacheKey cacheKey, T element, ITransaction? transaction = null, IRedLock? redisLock = null)
    {
        return await _cacheRepository.Save(cacheKey, element, transaction, redisLock);
    }
    
    // public Task<UpdateEntityResult<T>> SetElement(CacheKey cacheKey, T createdOrUpdatedElement, IDatabaseAsync database)
    // {
    //     // Cette méthode est maintenue pour compatibilité avec l'interface
    //     throw new NotImplementedException("Cette méthode est dépréciée. Utilisez Save à la place.");
    // }
    
    public async Task Delete(string key)
    {
        await Delete(key, null);
    }
    
    public async Task Delete(string key, ITransaction? transaction)
    {
        var cacheKey = _cacheService.ComputeCacheKey(EntityType, key);
        await _cacheRepository.Delete(cacheKey, transaction);
    }
}