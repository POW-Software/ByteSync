using ByteSync.ServerCommon.Business.Auth;
using ByteSync.ServerCommon.Security;
using ByteSync.Common.Business.Sessions.Cloud;
using MediatR;

namespace ByteSync.ServerCommon.Commands.SessionMembers;

public class GetMembersRequest : IRequest<List<SessionMemberInfoDTO>>, ISessionRequest
{
    public Client? Client { get; }

    public GetMembersRequest(string sessionId, Client? client = null)
    {
        Client = client;
        SessionId = sessionId;
    }
    
    public string SessionId { get; }
} 