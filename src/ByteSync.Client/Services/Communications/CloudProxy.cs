using ByteSync.Common.Business.EndPoints;
using ByteSync.Interfaces.Controls.Applications;
using ByteSync.Interfaces.Controls.Communications;
using ByteSync.Interfaces.Controls.Communications.SignalR;
using ByteSync.Interfaces.Services.Communications;

namespace ByteSync.Services.Communications;

public class CloudProxy : ICloudProxy
{
    private readonly IConnectionService _connectionService;

    public CloudProxy(IEnvironmentService environmentService, IHubPushHandler2 hubPushHandler2,
        IConnectionService connectionService)
    {
        HubPushHandler2 = hubPushHandler2;
        _connectionService = connectionService;
    }
    
    public IHubPushHandler2 HubPushHandler2 { get; }
    
    public ByteSyncEndpoint CurrentEndPoint => _connectionService.CurrentEndPoint!;
    
    public string ClientInstanceId => CurrentEndPoint.ClientInstanceId;
}