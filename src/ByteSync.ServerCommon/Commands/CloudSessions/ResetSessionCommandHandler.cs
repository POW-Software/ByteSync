using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class ResetSessionCommandHandler : IRequestHandler<ResetSessionRequest, Unit>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    public ResetSessionCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    public async Task<Unit> Handle(ResetSessionRequest request, CancellationToken cancellationToken)
    {
        await _cloudSessionsService.ResetSession(request.SessionId, request.Client);
        return Unit.Value;
    }
} 