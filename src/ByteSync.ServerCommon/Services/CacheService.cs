﻿using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Exceptions;
using ByteSync.ServerCommon.Interfaces.Factories;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Services;

public class CacheService : ICacheService
{
    private readonly RedisSettings _redisSettings;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private readonly RedLockFactory _redLockFactory;

    public CacheService(IOptions<RedisSettings> redisSettings, ICacheKeyFactory cacheKeyFactory, ILoggerFactory loggerFactory)
    {
        _redisSettings = redisSettings.Value;
        _cacheKeyFactory = cacheKeyFactory;
        
        _connectionMultiplexer = ConnectionMultiplexer.Connect(_redisSettings.ConnectionString);

        var multiplexers = new List<RedLockMultiplexer>
        {
            _connectionMultiplexer,
        };
        
        RedLockRetryConfiguration redLockRetryConfiguration = new RedLockRetryConfiguration(5, 500);
        _redLockFactory = RedLockFactory.Create(multiplexers, redLockRetryConfiguration, loggerFactory);
    }

    // public string Prefix => _redisSettings.Prefix;

    public ITransaction OpenTransaction()
    {
        return _connectionMultiplexer.GetDatabase().CreateTransaction();
    }

    public IDatabaseAsync GetDatabase()
    {
        return _connectionMultiplexer.GetDatabase();
    }
    
    public IDatabaseAsync GetDatabase(ITransaction? transaction)
    {
        IDatabaseAsync database = transaction != null ? transaction : GetDatabase();

        return database;
    }

    public async Task<IRedLock> AcquireLockAsync(EntityType entityType, string entityId)
    {
        var cacheKey = ComputeCacheKey(entityType, entityId);
        
        return await AcquireLockAsync(cacheKey);
    }

    public async Task<IRedLock> AcquireLockAsync(CacheKey cacheKey)
    {
        var redisLock = await _redLockFactory.CreateLockAsync(cacheKey.Value, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1));

        if (redisLock.IsAcquired)
        {
            return redisLock;
        }
        else
        {
            throw new AcquireRedisLockException(cacheKey.Value, redisLock);
        }
    }

    public CacheKey ComputeCacheKey(EntityType entityType, string entityId)
    {
        CacheKey cacheKey = _cacheKeyFactory.Create(entityType, entityId);

        return cacheKey;
    }
}