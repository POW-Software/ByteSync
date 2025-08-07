using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class GetDownloadFileUrlCommandHandler : IRequestHandler<GetDownloadFileUrlRequest, string>
{
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<GetDownloadFileUrlCommandHandler> _logger;

    public GetDownloadFileUrlCommandHandler(
        ITransferLocationService transferLocationService,
        ILogger<GetDownloadFileUrlCommandHandler> logger)
    {
        _transferLocationService = transferLocationService;
        _logger = logger;
    }
    
    public async Task<string> Handle(GetDownloadFileUrlRequest request, CancellationToken cancellationToken)
    {
        var url = await _transferLocationService.GetDownloadFileUrl(
            request.SessionId,
            request.Client,
            request.SharedFileDefinition,
            request.PartNumber
        );
        
        _logger.LogDebug("Download file URL generated for session {SessionId}, file {FileId}, part {PartNumber}", 
            request.SessionId, request.SharedFileDefinition.Id, request.PartNumber);
        
        return url;
    }
} 