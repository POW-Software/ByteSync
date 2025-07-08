using System.Collections.Generic;
using System.Threading.Tasks;
using ByteSync.Common.Business.Announcements;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface IAnnouncementApiClient
{
    Task<List<Announcement>> GetAnnouncements();
}
