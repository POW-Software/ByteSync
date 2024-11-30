using System.Threading.Tasks;
using ByteSync.Business.Profiles;
using ByteSync.Common.Business.Lobbies;
using ByteSync.Common.Business.SharedFiles;

namespace ByteSync.Interfaces.Profiles;

public interface ISessionProfileLocalDataManager
{
    string GetProfileZipPath(string profileId);
    
    string GetProfileZipPath(SharedFileDefinition sharedFileDefinition);

    Task<List<AbstractSessionProfile>> GetAllSavedProfiles();

    Task<CloudSessionProfileDetails> LoadCloudSessionProfileDetails(LobbyInfo lobbyInfo);
    
    Task<CloudSessionProfileDetails?> LoadCloudSessionProfileDetails(CloudSessionProfile cloudSessionProfile);

    Task<LocalSessionProfileDetails> LoadLocalSessionProfileDetails(LocalSessionProfile localSessionProfile);
    
    void DeleteSessionProfile(AbstractSessionProfile sessionProfile);
    
    void DeleteSessionProfile(AbstrastSessionProfileDetails cloudSessionProfileDetails);
}