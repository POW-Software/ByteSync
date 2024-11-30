using System.Threading.Tasks;
using ByteSync.Business.Communications;
using ByteSync.Business.Profiles;

namespace ByteSync.Interfaces.Profiles;

public interface ISessionProfileManager
{
    Task CreateCloudSessionProfile(string sessionId, string profileName, CloudSessionProfileOptions cloudSessionProfileOptions);
    
    Task CreateLocalSessionProfile(string sessionId, string profileName, LocalSessionProfileOptions localSessionProfileOptions);
    
    Task OnFileIsFullyDownloaded(LocalSharedFile localSharedFile);
    
    Task<bool> DeleteSessionProfile(AbstractSessionProfile cloudSessionProfile);
}