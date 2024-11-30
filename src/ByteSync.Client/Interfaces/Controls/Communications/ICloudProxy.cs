using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Communications.SignalR;

namespace ByteSync.Interfaces.Controls.Communications;

public interface ICloudProxy
{
    IHubPushHandler2 HubPushHandler2 { get; }
    ByteSyncEndpoint CurrentEndPoint { get; }
    string ClientInstanceId { get; }
}