using ByteSync.ServerCommon.Business.Settings;
using Microsoft.Extensions.Configuration;

namespace ByteSync.ServerCommon.Tests.Helpers;

public class TestSettingsInitializer
{
    private static IConfiguration? _config;
    
    private static RedisSettings? _redisSettings;

    public IConfiguration InitConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("server-common-tests.local.settings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        return config;
    }

    public static RedisSettings GetRedisSettings()
    {
        if (_redisSettings == null)
        {
            _config ??= new TestSettingsInitializer().InitConfiguration();
            
            var redisSettings = _config.GetSection("Redis").Get<RedisSettings>();
            redisSettings!.Prefix = "test";

            _redisSettings = redisSettings;
        }
        
        return _redisSettings;
    }
}