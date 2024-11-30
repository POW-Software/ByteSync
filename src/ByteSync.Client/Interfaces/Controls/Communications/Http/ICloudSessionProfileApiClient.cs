using System.Threading.Tasks;
using ByteSync.Common.Business.Profiles;

namespace ByteSync.Interfaces.Controls.Communications.Http;

public interface ICloudSessionProfileApiClient
{
    Task<CreateCloudSessionProfileResult> CreateCloudSessionProfile(string sessionId);

    Task<CloudSessionProfileData> GetCloudSessionProfileData(string sessionId, string additionalName);

    Task<bool> DeleteCloudSessionProfile(DeleteCloudSessionProfileParameters parameters);
}