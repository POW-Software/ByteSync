using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting;

public interface ICreateSessionService
{
    Task<CloudSessionResult?> CreateCloudSession(CreateCloudSessionRequest request);
    
    Task CancelCreateCloudSession();
}