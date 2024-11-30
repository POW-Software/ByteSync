using ByteSync.Common.Business.EndPoints;
using ByteSync.Common.Business.Serials;

namespace ByteSync.Common.Business.Auth;

public class InitialAuthenticationResponse
{
    public InitialAuthenticationResponse()
    {
        
    }
    
    public InitialAuthenticationResponse(InitialConnectionStatus initialConnectionStatus)
    {
        InitialConnectionStatus = initialConnectionStatus;
        EndPoint = null;
        BindSerialResponse = new BindSerialResponse {Status = BindSerialResponseStatus.Ignored};
    }
    
    public InitialAuthenticationResponse(InitialConnectionStatus initialConnectionStatus,
        ByteSyncEndpoint? byteSyncEndpoint, AuthenticationTokens? authenticationTokens,
        BindSerialResponse bindSerialResponse)
    {
        InitialConnectionStatus = initialConnectionStatus;
        EndPoint = byteSyncEndpoint;
        BindSerialResponse = bindSerialResponse;
        AuthenticationTokens = authenticationTokens;
    }

    public InitialConnectionStatus InitialConnectionStatus { get; set; }
    
    public ByteSyncEndpoint? EndPoint { get; set; }
    
    public BindSerialResponse BindSerialResponse { get; set; }
    
    public AuthenticationTokens? AuthenticationTokens { get; set; }
    
    public bool IsSuccess
    {
        get => InitialConnectionStatus == InitialConnectionStatus.Success;
    }
}