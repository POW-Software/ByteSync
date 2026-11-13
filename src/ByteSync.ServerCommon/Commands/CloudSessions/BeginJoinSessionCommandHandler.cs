using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class BeginJoinSessionCommandHandler(ICloudSessionsRepository sessions, ICloudSessionsService service)
    : IRequestHandler<BeginJoinSessionRequest, List<string>>
{
    public async Task<List<string>> Handle(BeginJoinSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await sessions.Get(request.SessionId) ?? throw new UnauthorizedAccessException();
        if (session.IsSessionActivated || session.IsSessionRemoved || session.SessionMembers.Count is 0 or >= 5) throw new UnauthorizedAccessException();
        await service.PreJoinCloudSession(request.Client, request.Parameters.PublicKeyInfo, request.Parameters.ProfileClientId,
            request.SessionId, session.SessionMembers[0].ClientInstanceId);
        return session.SessionMembers.Select(m => m.ClientInstanceId).ToList();
    }
}
