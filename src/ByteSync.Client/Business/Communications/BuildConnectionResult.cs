using ByteSync.Common.Business.Auth;
using ByteSync.Common.Business.EndPoints;
using Microsoft.AspNetCore.SignalR.Client;

namespace ByteSync.Business.Communications;

public record BuildConnectionResult
{
    public ByteSyncEndpoint? EndPoint { get; init; }
    
    public HubConnection? HubConnection { get; init; }
    
    public InitialConnectionStatus? AuthenticateResponseStatus { get; set; }
}