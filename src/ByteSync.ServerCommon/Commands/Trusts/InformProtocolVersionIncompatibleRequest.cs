using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class InformProtocolVersionIncompatibleRequest : IRequest, ISessionRequest
{
    public string SessionId => Parameters.SessionId;

    public InformProtocolVersionIncompatibleRequest(InformProtocolVersionIncompatibleParameters parameters, Client client)
    {
        Parameters = parameters;
        Client = client;
    }

    public InformProtocolVersionIncompatibleParameters Parameters { get; set; }
    
    public Client Client { get; set; }
}

