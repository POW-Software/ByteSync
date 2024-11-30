using ByteSync.Common.Business.Lobbies.Connections;
using ByteSync.Common.Business.Profiles;
using ByteSync.ServerCommon.Business.Auth;

namespace ByteSync.ServerCommon.Interfaces.Services;

public interface ICloudSessionProfileService
{
    Task<CreateCloudSessionProfileResult> CreateCloudSessionProfile(string sessionId, Client client);
    
    Task<CloudSessionProfileData?> GetCloudSessionProfileData(GetCloudSessionProfileDataParameters getCloudSessionProfileDataParameters,
        Client client);

    Task<string?> GetProfileDetailsPassword(GetProfileDetailsPasswordParameters parameters, Client client);
    
    Task<bool> DeleteCloudSessionProfile(DeleteCloudSessionProfileParameters parameters, Client client);
}