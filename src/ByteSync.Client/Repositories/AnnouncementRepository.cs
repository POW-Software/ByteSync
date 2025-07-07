using ByteSync.Common.Business.Announcements;
using ByteSync.Interfaces.Repositories;

namespace ByteSync.Repositories;

public class AnnouncementRepository : BaseSourceCacheRepository<Announcement, string>, IAnnouncementRepository
{
    protected override string KeySelector(Announcement announcement) => announcement.Id;
}
