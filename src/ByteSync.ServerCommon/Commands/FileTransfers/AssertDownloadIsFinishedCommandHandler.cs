using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertDownloadIsFinishedCommandHandler : IRequestHandler<AssertDownloadIsFinishedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISynchronizationService _synchronizationService;
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<AssertDownloadIsFinishedCommandHandler> _logger;

    public AssertDownloadIsFinishedCommandHandler(ICloudSessionsRepository cloudSessionsRepository,
        ISynchronizationService synchronizationService,
        ITransferLocationService transferLocationService,
        ILogger<AssertDownloadIsFinishedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _synchronizationService = synchronizationService;
        _transferLocationService = transferLocationService;
        _logger = logger;
    }
    
    public async Task Handle(AssertDownloadIsFinishedRequest request, CancellationToken cancellationToken)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(request.SessionId, request.Client);
        var sharedFileDefinition = request.TransferParameters.SharedFileDefinition;

        if (_transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            _logger.LogInformation("AssertDownloadIsFinished: {cloudSession} {sharedFileDefinition}",
                sessionMemberData!.CloudSessionData.SessionId,
                sharedFileDefinition.Id);

            if (sharedFileDefinition.IsSynchronization)
            {
                await _synchronizationService.OnDownloadIsFinishedAsync(sharedFileDefinition, request.Client);
            }
        }
        
        _logger.LogDebug("Download finished asserted for session {SessionId}, file {FileId}", 
            request.SessionId, sharedFileDefinition.Id);
    }
} 