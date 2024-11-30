using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class CloudSessionProfileRepository : BaseRepository<CloudSessionProfileEntity>, ICloudSessionProfileRepository
{
    public CloudSessionProfileRepository(ICacheService cacheService) : base(cacheService)
    {
    }

    public override string ElementName { get; } = "CloudSessionProfile";
}