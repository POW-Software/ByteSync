using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfer;

public class AssertFilePartIsDownloadedCommandHandler : IRequestHandler<AssertFilePartIsDownloadedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly ILogger<AssertFilePartIsDownloadedCommandHandler> _logger;
    private readonly ITransferLocationService _transferLocationService;

    public AssertFilePartIsDownloadedCommandHandler(
        ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ILogger<AssertFilePartIsDownloadedCommandHandler> logger,
        ITransferLocationService transferLocationService)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _logger = logger;
        _transferLocationService = transferLocationService;
    }
    
    public async Task Handle(AssertFilePartIsDownloadedRequest request, CancellationToken cancellationToken)
    {
        var sessionMemberData = await _cloudSessionsRepository.GetSessionMember(request.SessionId, request.Client);

        if (_transferLocationService.IsSharedFileDefinitionAllowed(sessionMemberData, request.SharedFileDefinition))
        {
            await _sharedFilesService.AssertFilePartIsDownloaded(request.SharedFileDefinition, request.Client, request.PartNumber);
        }
        
        _logger.LogDebug("File part download asserted for session {SessionId}, file {FileId}, part {PartNumber}", 
            request.SessionId, request.SharedFileDefinition.Id, request.PartNumber);
    }
} 