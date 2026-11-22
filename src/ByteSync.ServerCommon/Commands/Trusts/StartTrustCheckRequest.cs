using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Trusts;

public class StartTrustCheckRequest : IRequest<StartTrustCheckResult>, ISessionRequest
{
    public string SessionId => Parameters.SessionId;

    public StartTrustCheckRequest(TrustCheckParameters parameters, Client client)
    {
        Parameters = parameters;
        Client = client;
    }

    public TrustCheckParameters Parameters { get; set; }
    
    public Client Client { get; set; }
}