using System.Text;
using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Factories;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Factories;

public class CacheKeyFactory : ICacheKeyFactory
{
    private readonly string _prefix;

    public CacheKeyFactory(IOptions<RedisSettings> redisSettings)
    {
        _prefix = redisSettings.Value.Prefix;
    }
    
    public CacheKey Create(EntityType entityType, string entityId)
    {
        string entityTypeName = GetEntityTypeName(entityType);

        StringBuilder sb = new StringBuilder(_prefix);
        sb.Append(':');
        sb.Append(entityTypeName);
        sb.Append(':');
        sb.Append(entityId);
        var cacheKeyValue = sb.ToString();

        var cacheKey = new CacheKey
        {
            EntityType = entityType,
            EntityId = entityId,
            Value = cacheKeyValue
        };

        return cacheKey;
    }

    private string GetEntityTypeName(EntityType entityType)
    {
        return entityType switch
        {
            EntityType.Session => "Session",
            EntityType.Inventory => "Inventory",
            EntityType.Synchronization => "Synchronization",
            EntityType.SharedFile => "SharedFile",
            EntityType.SessionSharedFiles => "SessionSharedFiles",
            EntityType.TrackingAction => "TrackingAction",
            EntityType.Client => "Client",
            EntityType.ClientSoftwareVersionSettings => "ClientSoftwareVersionSettings",
            EntityType.CloudSessionProfile => "CloudSessionProfile",
            EntityType.Lobby => "Lobby",
            EntityType.Announcement => "Announcement",
            _ => throw new ArgumentOutOfRangeException(nameof(entityType), entityType, null)
        };
    }
}