using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Security;
using MediatR;

namespace ByteSync.ServerCommon.Commands.SessionMembers;

public class GetMembersInstanceIdsRequest : IRequest<List<string>>, ISessionRequest
{
    public Client? Client { get; }

    public GetMembersInstanceIdsRequest(string sessionId, Client? client = null)
    {
        Client = client;
        SessionId = sessionId;
    }
    
    public string SessionId { get; }
} 