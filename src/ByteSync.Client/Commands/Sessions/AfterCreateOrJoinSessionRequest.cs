using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using MediatR;

namespace ByteSync.Commands.Sessions;

public class AfterCreateOrJoinSessionRequest : IRequest
{
    public AfterCreateOrJoinSessionRequest(CloudSessionResult cloudSessionResult, RunCloudSessionProfileInfo? runCloudSessionProfileInfo, 
        bool isCreator)
    {
        CloudSessionResult = cloudSessionResult;
        RunCloudSessionProfileInfo = runCloudSessionProfileInfo;
        IsCreator = isCreator;
    }
    
    public CloudSessionResult CloudSessionResult { get; }
    
    public RunCloudSessionProfileInfo? RunCloudSessionProfileInfo { get; }
    
    public bool IsCreator { get; }
}