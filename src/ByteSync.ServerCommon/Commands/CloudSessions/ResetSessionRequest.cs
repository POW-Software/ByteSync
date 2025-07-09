using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class ResetSessionRequest : IRequest<Unit>
{
    public ResetSessionRequest(string sessionId, Client client)
    {
        SessionId = sessionId;
        Client = client;
    }
    public string SessionId { get; }
    
    public Client Client { get; }
} 