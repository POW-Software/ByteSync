using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Inventories;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.Interfaces.Commands.Sessions;
using MediatR;

namespace ByteSync.Commands.Sessions;

public class CreateSessionRequest : IRequest<CloudSessionResult?>
{
    public CreateSessionRequest(RunCloudSessionProfileInfo? runCloudSessionProfileInfo)
    {
        RunCloudSessionProfileInfo = runCloudSessionProfileInfo;
    }

    public RunCloudSessionProfileInfo? RunCloudSessionProfileInfo { get; set; }
}