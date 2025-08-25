using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class RequestTrustPublicKeyRequest: IRequest
{
    public RequestTrustPublicKeyRequest(RequestTrustProcessParameters parameters, Client client)
    {
        Parameters = parameters;
        Client = client;        
    }

    public RequestTrustProcessParameters Parameters { get; set; }
        
    public Client Client { get; set; }
}