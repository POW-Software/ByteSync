using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class GiveMemberPublicKeyCheckDataRequest : IRequest
{
    public GiveMemberPublicKeyCheckDataRequest(GiveMemberPublicKeyCheckDataParameters parameters, Client client)
    {
        Parameters = parameters;
        Client = client;
    }

    public GiveMemberPublicKeyCheckDataParameters Parameters { get; set; }
    
    public Client Client { get; set; }
}