using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class RequestSynchronizationAbortRequest : IRequest
{
    public RequestSynchronizationAbortRequest(string sessionId, Client client)
    {
        SessionId = sessionId;
        Client = client;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
}