using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class SetAuthCheckedRequest: IRequest
{
    public SetAuthCheckedRequest(SetAuthCheckedParameters parameters, Client client)
    {
        Parameters = parameters;
        Client = client;        
    }

    public SetAuthCheckedParameters Parameters { get; set; }
        
    public Client Client { get; set; }
}