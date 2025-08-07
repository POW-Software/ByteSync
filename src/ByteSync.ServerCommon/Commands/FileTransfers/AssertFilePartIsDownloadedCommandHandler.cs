using ByteSync.ServerCommon.Interfaces.Repositories;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class AssertFilePartIsDownloadedCommandHandler : IRequestHandler<AssertFilePartIsDownloadedRequest>
{
    private readonly ICloudSessionsRepository _cloudSessionsRepository;
    private readonly ISharedFilesService _sharedFilesService;
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<AssertFilePartIsDownloadedCommandHandler> _logger;

    public AssertFilePartIsDownloadedCommandHandler(ICloudSessionsRepository cloudSessionsRepository,
        ISharedFilesService sharedFilesService,
        ITransferLocationService transferLocationService,
        ILogger<AssertFilePartIsDownloadedCommandHandler> logger)
    {
        _cloudSessionsRepository = cloudSessionsRepository;
        _sharedFilesService = sharedFilesService;
        _transferLocationService = transferLocationService;
        _logger = logger;
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