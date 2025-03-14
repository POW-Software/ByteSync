using ByteSync.Common.Business.Trust.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class SendDigitalSignaturesRequest : IRequest
{
    public SendDigitalSignaturesRequest(SendDigitalSignaturesParameters parameters, Client client)
    {
        Parameters = parameters;
        Client = client;
    }

    public SendDigitalSignaturesParameters Parameters { get; set; }
    
    public Client Client { get; set; }
}