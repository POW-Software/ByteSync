using ByteSync.Business.Sessions.RunSessionInfos;

namespace ByteSync.Business.Sessions;

public class CreateSessionRequest
{
    public CreateSessionRequest(RunCloudSessionProfileInfo? runCloudSessionProfileInfo)
    {
        RunCloudSessionProfileInfo = runCloudSessionProfileInfo;
    }

    public RunCloudSessionProfileInfo? RunCloudSessionProfileInfo { get; set; }
}