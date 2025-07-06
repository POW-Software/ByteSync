using ByteSync.ServerCommon.Business.Announcements;
using ByteSync.ServerCommon.Entities;
using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;

namespace ByteSync.ServerCommon.Repositories;

public class AnnouncementRepository : BaseRepository<List<Announcement>>, IAnnouncementRepository
{
    public const string UniqueKey = "All";

    public AnnouncementRepository(IRedisInfrastructureService redisInfrastructureService, ICacheRepository<List<Announcement>> cacheRepository)
        : base(redisInfrastructureService, cacheRepository)
    {
    }

    public override EntityType EntityType => EntityType.Announcement;

    public Task<List<Announcement>?> GetAll()
    {
        return Get(UniqueKey);
    }

    public Task SaveAll(List<Announcement> announcements)
    {
        return Save(UniqueKey, announcements);
    }
}
