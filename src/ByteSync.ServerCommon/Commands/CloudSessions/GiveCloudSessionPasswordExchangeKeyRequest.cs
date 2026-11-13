using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class GiveCloudSessionPasswordExchangeKeyRequest : IRequest<Unit>, ISessionRequest
{
    public string SessionId => Parameters.SessionId;

    public GiveCloudSessionPasswordExchangeKeyRequest(Client client, GiveCloudSessionPasswordExchangeKeyParameters parameters)
    {
        Client = client;
        Parameters = parameters;
    }

    public Client Client { get; }
    
    public GiveCloudSessionPasswordExchangeKeyParameters Parameters { get; }
} 