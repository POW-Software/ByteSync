using ByteSync.Business.Sessions.RunSessionInfos;

namespace ByteSync.Business.Sessions;

public class CreateCloudSessionRequest
{
    public CreateCloudSessionRequest(RunCloudSessionProfileInfo? runCloudSessionProfileInfo)
    {
        RunCloudSessionProfileInfo = runCloudSessionProfileInfo;
    }

    public RunCloudSessionProfileInfo? RunCloudSessionProfileInfo { get; set; }
}