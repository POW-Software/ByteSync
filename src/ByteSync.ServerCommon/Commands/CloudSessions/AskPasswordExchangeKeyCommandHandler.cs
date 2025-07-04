using ByteSync.Common.Business.Sessions.Cloud.Connections;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class AskPasswordExchangeKeyCommandHandler : IRequestHandler<AskPasswordExchangeKeyRequest, JoinSessionResult>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    public AskPasswordExchangeKeyCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    public async Task<JoinSessionResult> Handle(AskPasswordExchangeKeyRequest request, CancellationToken cancellationToken)
    {
        return await _cloudSessionsService.AskCloudSessionPasswordExchangeKey(request.Client, request.Parameters);
    }
} 