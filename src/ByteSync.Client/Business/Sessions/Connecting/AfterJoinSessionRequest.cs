using ByteSync.Business.Sessions.RunSessionInfos;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.Business.Sessions.Connecting;

public class AfterJoinSessionRequest
{
    public AfterJoinSessionRequest(CloudSessionResult cloudSessionResult, RunCloudSessionProfileInfo? runCloudSessionProfileInfo, 
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