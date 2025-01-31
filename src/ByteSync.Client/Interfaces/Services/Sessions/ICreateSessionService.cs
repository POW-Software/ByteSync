using System.Threading;
using System.Threading.Tasks;
using ByteSync.Business.Sessions;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions;

public interface ICreateSessionService
{
    Task<CloudSessionResult?> CreateCloudSession(CreateCloudSessionRequest request);
    
    Task CancelCreateCloudSession();
}