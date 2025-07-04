using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class ValidateJoinCloudSessionCommandHandler : IRequestHandler<ValidateJoinCloudSessionRequest, Unit>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    public ValidateJoinCloudSessionCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    public async Task<Unit> Handle(ValidateJoinCloudSessionRequest request, CancellationToken cancellationToken)
    {
        await _cloudSessionsService.ValidateJoinCloudSession(request.Parameters);
        return Unit.Value;
    }
} 