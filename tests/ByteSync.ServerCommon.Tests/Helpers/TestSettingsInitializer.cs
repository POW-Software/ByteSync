using Azure.Identity;
using ByteSync.ServerCommon.Business.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace ByteSync.ServerCommon.Tests.Helpers;

public class TestSettingsInitializer
{
    private static IConfiguration? _config;
    
    private static RedisSettings? _redisSettings;
    private static CosmosDbSettings? _cosmosDbSettings;

    public IConfiguration InitConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Secret.json", optional: false) // Secret configuration
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

    public static CosmosDbSettings GetCosmosDbSettings()
    {
        if (_cosmosDbSettings == null)
        {
            _config ??= new TestSettingsInitializer().InitConfiguration();
            
            var cosmosDbSettings = _config.GetSection("CosmosDb").Get<CosmosDbSettings>();

            _cosmosDbSettings = cosmosDbSettings;
        }
        
        return _cosmosDbSettings;
    }
}