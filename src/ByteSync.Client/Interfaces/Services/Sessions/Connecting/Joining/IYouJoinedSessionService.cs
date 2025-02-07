using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Interfaces.Services.Sessions.Connecting.Joining;

public interface IYouJoinedSessionService
{
    Task Process(CloudSessionResult cloudSessionResult, ValidateJoinCloudSessionParameters parameters);
}