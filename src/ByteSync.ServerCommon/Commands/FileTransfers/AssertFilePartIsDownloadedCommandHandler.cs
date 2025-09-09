using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertFilePartIsDownloadedCommandHandler : IRequestHandler<AssertFilePartIsDownloadedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly ISynchronizationRepository _synchronizationRepository;
    private readonly ISynchronizationStatusCheckerService _synchronizationStatusCheckerService;
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<AssertFilePartIsDownloadedCommandHandler> _logger;

    public AssertFilePartIsDownloadedCommandHandler(ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ISynchronizationRepository synchronizationRepository,
        ISynchronizationStatusCheckerService synchronizationStatusCheckerService,
        ITransferLocationService transferLocationService,
        ILogger<AssertFilePartIsDownloadedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _synchronizationRepository = synchronizationRepository;
        _synchronizationStatusCheckerService = synchronizationStatusCheckerService;
        _transferLocationService = transferLocationService;
        _logger = logger;
    }
    
    public async Task Handle(AssertFilePartIsDownloadedRequest request, CancellationToken cancellationToken)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(request.SessionId, request.Client);
        var sharedFileDefinition = request.TransferParameters.SharedFileDefinition;
        var partNumber = request.TransferParameters.PartNumber!.Value;

        if (_transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, sharedFileDefinition))
        {
            await _sharedFilesService.AssertFilePartIsDownloaded(request.Client, request.TransferParameters);
            
            // Track downloaded volume if part size is provided
            if (request.TransferParameters.PartSizeInBytes.HasValue && sharedFileDefinition.IsSynchronization)
            {
                await _synchronizationRepository.UpdateIfExists(sharedFileDefinition.SessionId, synchronization =>
                {
                    if (_synchronizationStatusCheckerService.CheckSynchronizationCanBeUpdated(synchronization))
                    {
                        synchronization.Progress.ActualDownloadedVolume += request.TransferParameters.PartSizeInBytes.Value;
                        return true;
                    }
                    return false;
                });
            }
        }
        
        _logger.LogDebug("File part download asserted for session {SessionId}, file {FileId}, part {PartNumber}", 
            request.SessionId, sharedFileDefinition.Id, partNumber);
    }
} 