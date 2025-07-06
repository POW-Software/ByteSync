using ByteSync.ServerCommon.Business.Announcements;

namespace ByteSync.ServerCommon.Interfaces.Repositories;

public interface IAnnouncementRepository : IRepository<List<Announcement>>
{
    Task<List<Announcement>?> GetAll();

    Task SaveAll(List<Announcement> announcements);
}
