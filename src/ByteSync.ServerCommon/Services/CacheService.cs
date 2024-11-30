using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace ByteSync.ServerCommon.Services;

public class CacheService : ICacheService
{
    private readonly RedisSettings _redisSettings;
    private readonly ConnectionMultiplexer _connectionMultiplexer;
    private readonly RedLockFactory _redLockFactory;

    public CacheService(IOptions<RedisSettings> redisSettings, ILoggerFactory loggerFactory)
    {
        _redisSettings = redisSettings.Value;
        
        _connectionMultiplexer = ConnectionMultiplexer.Connect(_redisSettings.ConnectionString);

        var multiplexers = new List<RedLockMultiplexer>
        {
            _connectionMultiplexer,
        };
        
        RedLockRetryConfiguration redLockRetryConfiguration = new RedLockRetryConfiguration(5, 500);
        _redLockFactory = RedLockFactory.Create(multiplexers, redLockRetryConfiguration, loggerFactory);
    }
    
    public RedLockFactory RedLockFactory
    {
        get
        {
            return _redLockFactory;
        }
    }

    public string Prefix => _redisSettings.Prefix;

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

    public async Task<IAsyncDisposable> AcquireLockAsync(string key)
    {
        var sessionSharedFilesLock = await _redLockFactory.CreateLockAsync(key, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(1));

        if (sessionSharedFilesLock.IsAcquired)
        {
            return sessionSharedFilesLock;
        }
        else
        {
            throw new Exception("Could not acquire redis lock for key: " + key);
        }
    }
}