using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class GetDownloadFileStorageLocationCommandHandler : IRequestHandler<GetDownloadFileStorageLocationRequest, FileStorageLocation>
{
    private readonly ITransferLocationService _transferLocationService;
    private readonly StorageProvider _storageProvider;
    private readonly ILogger<GetDownloadFileStorageLocationCommandHandler> _logger;

    public GetDownloadFileStorageLocationCommandHandler(
        ITransferLocationService transferLocationService,
        IOptions<AppSettings> appSettings,
        ILogger<GetDownloadFileStorageLocationCommandHandler> logger)
    {
        _transferLocationService = transferLocationService;
        _storageProvider = appSettings.Value.DefaultStorageProvider;
        _logger = logger;
    }
    
    public async Task<FileStorageLocation> Handle(GetDownloadFileStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var url = await _transferLocationService.GetDownloadFileUrl(
            request.SessionId,
            request.Client,
            request.TransferParameters.SharedFileDefinition,
            request.TransferParameters.PartNumber!.Value
        );
        
        var responseObject = new FileStorageLocation(url, _storageProvider);
        
        _logger.LogDebug("Download file storage location generated for session {SessionId}, file {FileId}", 
            request.SessionId, request.TransferParameters.SharedFileDefinition.Id);
        
        return responseObject;
    }
} 