using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class CloudSessionProfileRepository : BaseRepository<CloudSessionProfileEntity>, ICloudSessionProfileRepository
{
    public CloudSessionProfileRepository(IRedisInfrastructureService redisInfrastructureService,
        ICacheRepository<CloudSessionProfileEntity> cacheRepository) : base(redisInfrastructureService, cacheRepository)
    {
    }

    public override EntityType EntityType { get; } = EntityType.CloudSessionProfile;
}