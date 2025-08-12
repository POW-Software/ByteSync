using ByteSync.ServerCommon.Business.Auth;
using MediatR;

namespace ByteSync.ServerCommon.Commands.Synchronizations;

public class MemberHasFinishedRequest : IRequest
{
    public MemberHasFinishedRequest(string sessionId, Client client)
    {
        SessionId = sessionId;
        Client = client;
    }

    public string SessionId { get; set; }
    public Client Client { get; set; }
}