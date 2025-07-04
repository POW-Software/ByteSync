using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class InformPasswordIsWrongCommandHandler : IRequestHandler<InformPasswordIsWrongRequest, Unit>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    public InformPasswordIsWrongCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    public async Task<Unit> Handle(InformPasswordIsWrongRequest request, CancellationToken cancellationToken)
    {
        await _cloudSessionsService.InformPasswordIsWrong(request.Client, request.SessionId, request.ClientInstanceId);
        return Unit.Value;
    }
} 