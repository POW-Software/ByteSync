using ByteSync.ServerCommon.Business.Repositories;
using ByteSync.ServerCommon.Entities;

namespace ByteSync.ServerCommon.Interfaces.Factories;

public interface ICacheKeyFactory
{
    CacheKey Create(EntityType entityType, string entityId);
}