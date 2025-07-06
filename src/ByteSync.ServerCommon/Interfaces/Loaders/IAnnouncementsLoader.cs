using ByteSync.Common.Business.Announcements;

namespace ByteSync.ServerCommon.Interfaces.Loaders;

public interface IAnnouncementsLoader
{
    Task<List<Announcement>> Load();
}
