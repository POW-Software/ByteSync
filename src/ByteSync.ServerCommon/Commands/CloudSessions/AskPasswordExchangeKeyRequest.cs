using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class AskPasswordExchangeKeyRequest : IRequest<JoinSessionResult>, ISessionRequest
{
    public string SessionId => Parameters.SessionId;

    public AskPasswordExchangeKeyRequest(Client client, AskCloudSessionPasswordExchangeKeyParameters parameters)
    {
        Client = client;
        Parameters = parameters;
    }

    public Client Client { get; }
    
    public AskCloudSessionPasswordExchangeKeyParameters Parameters { get; }
} 