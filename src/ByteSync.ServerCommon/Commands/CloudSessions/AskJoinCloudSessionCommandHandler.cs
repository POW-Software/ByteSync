using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class AskJoinCloudSessionCommandHandler : IRequestHandler<AskJoinCloudSessionRequest, JoinSessionResult>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    
    public AskJoinCloudSessionCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    
    public async Task<JoinSessionResult> Handle(AskJoinCloudSessionRequest request, CancellationToken cancellationToken)
    {
        return await _cloudSessionsService.AskJoinCloudSession(request.Client, request.Parameters);
    }
} 