using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using ByteSync.Common.Business.Sessions.Cloud.Connections;

namespace ByteSync.ServerCommon.Commands.CloudSessions;

public class GiveCloudSessionPasswordExchangeKeyCommandHandler : IRequestHandler<GiveCloudSessionPasswordExchangeKeyRequest, Unit>
{
    private readonly ICloudSessionsService _cloudSessionsService;
    
    public GiveCloudSessionPasswordExchangeKeyCommandHandler(ICloudSessionsService cloudSessionsService)
    {
        _cloudSessionsService = cloudSessionsService;
    }
    
    public async Task<Unit> Handle(GiveCloudSessionPasswordExchangeKeyRequest request, CancellationToken cancellationToken)
    {
        await _cloudSessionsService.GiveCloudSessionPasswordExchangeKey(request.Client, request.Parameters);
        return Unit.Value;
    }
} 