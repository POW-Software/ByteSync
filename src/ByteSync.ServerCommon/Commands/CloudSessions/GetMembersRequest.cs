using ByteSync.Common.Business.Sessions.Cloud;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class GetMembersRequest : IRequest<List<SessionMemberInfoDTO>>
{
    public GetMembersRequest(string sessionId)
    {
        SessionId = sessionId;
    }
    
    public string SessionId { get; }
} 