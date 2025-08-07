using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class GetUploadFileUrlCommandHandler : IRequestHandler<GetUploadFileUrlRequest, string>
{
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<GetUploadFileUrlCommandHandler> _logger;

    public GetUploadFileUrlCommandHandler(
        ITransferLocationService transferLocationService,
        ILogger<GetUploadFileUrlCommandHandler> logger)
    {
        _transferLocationService = transferLocationService;
        _logger = logger;
    }
    
    public async Task<string> Handle(GetUploadFileUrlRequest request, CancellationToken cancellationToken)
    {
        var url = await _transferLocationService.GetUploadFileUrl(
            request.SessionId,
            request.Client,
            request.SharedFileDefinition,
            request.PartNumber
        );
        
        _logger.LogDebug("Upload file URL generated for session {SessionId}, file {FileId}, part {PartNumber}", 
            request.SessionId, request.SharedFileDefinition.Id, request.PartNumber);
        
        return url;
    }
} 