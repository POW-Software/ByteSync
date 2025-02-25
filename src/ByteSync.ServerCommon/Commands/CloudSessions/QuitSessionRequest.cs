using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class QuitSessionRequest : IRequest
{
    public QuitSessionRequest(string sessionId, Client client)
    {
        SessionId = sessionId;
        Client = client;
    }
    
    public string SessionId { get; }
    
    public Client Client { get; }
    
    public string ClientInstanceId => Client.ClientInstanceId;
}