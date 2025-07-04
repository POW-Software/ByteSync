using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class GetMembersInstanceIdsRequest : IRequest<List<string>>
{
    public GetMembersInstanceIdsRequest(string sessionId)
    {
        SessionId = sessionId;
    }
    public string SessionId { get; }
} 