using ByteSync.ServerCommon.Business.Repositories;
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

public class RedisInfrastructureService : IRedisInfrastructureService
{
    private readonly RedisSettings _redisSettings;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly RedLockFactory _redLockFactory;
    private static string? _cachedConnectionString;
    private readonly ConnectionMultiplexer _connectionMultiplexer;

    public RedisInfrastructureService(IOptions<RedisSettings> redisSettings, ICacheKeyFactory cacheKeyFactory, ILoggerFactory loggerFactory)
    {
        _redisSettings = redisSettings.Value;
        _cacheKeyFactory = cacheKeyFactory;
        
        _cachedConnectionString ??= _redisSettings.ConnectionString;

        _connectionMultiplexer = _lazyMultiplexer.Value;

        var multiplexers = new List<RedLockMultiplexer>
        {
            _connectionMultiplexer,
        };
        
        RedLockRetryConfiguration redLockRetryConfiguration = new RedLockRetryConfiguration(10, 1000);
        _redLockFactory = RedLockFactory.Create(multiplexers, redLockRetryConfiguration, loggerFactory);
    }
    
    private static readonly Lazy<ConnectionMultiplexer> _lazyMultiplexer = new(() =>
    {
        var options = ConfigurationOptions.Parse(_cachedConnectionString!);
        
        options.Ssl = true;
        options.AbortOnConnectFail = false;
        
        if (options.ConnectTimeout < 10000)
        {
            options.ConnectTimeout = 10000;
        }
        
        return ConnectionMultiplexer.Connect(options);
    });

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
        var redisLock = await _redLockFactory.CreateLockAsync(cacheKey.Value, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(2));
        
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