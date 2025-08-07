using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertDownloadIsFinishedCommandHandler : IRequestHandler<AssertDownloadIsFinishedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ILogger<AssertDownloadIsFinishedCommandHandler> _logger;
    private readonly ITransferLocationService _transferLocationService;

    public AssertDownloadIsFinishedCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ISynchronizationService synchronizationService,
        ILogger<AssertDownloadIsFinishedCommandHandler> logger,
        ITransferLocationService transferLocationService)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _synchronizationService = synchronizationService;
        _logger = logger;
        _transferLocationService = transferLocationService;
    }
    
    public async Task Handle(AssertDownloadIsFinishedRequest request, CancellationToken cancellationToken)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(request.SessionId, request.Client);

        if (_transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, request.SharedFileDefinition))
        {
            _logger.LogInformation("AssertDownloadIsFinished: {cloudSession} {sharedFileDefinition}",
                sessionMemberData!.CloudSessionData.SessionId,
                request.SharedFileDefinition.Id);

            if (request.SharedFileDefinition.IsSynchronization)
            {
                await _synchronizationService.OnDownloadIsFinishedAsync(request.SharedFileDefinition, request.Client);
            }
        }
        
        _logger.LogDebug("Download finished asserted for session {SessionId}, file {FileId}", 
            request.SessionId, request.SharedFileDefinition.Id);
    }
} 