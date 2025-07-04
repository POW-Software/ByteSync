using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class InformPasswordIsWrongRequest : IRequest<Unit>
{
    public InformPasswordIsWrongRequest(Client client, string sessionId, string clientInstanceId)
    {
        Client = client;
        SessionId = sessionId;
        ClientInstanceId = clientInstanceId;
    }
    public Client Client { get; }
    public string SessionId { get; }
    public string ClientInstanceId { get; }
} 