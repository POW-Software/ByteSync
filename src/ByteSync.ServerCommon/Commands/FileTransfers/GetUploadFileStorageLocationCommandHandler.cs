using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Commands.FileTransfers;

public class GetUploadFileStorageLocationCommandHandler : IRequestHandler<GetUploadFileStorageLocationRequest, FileStorageLocation>
{
    private readonly ITransferLocationService _transferLocationService;
    private readonly ILogger<GetUploadFileStorageLocationCommandHandler> _logger;
    private readonly StorageProvider _storageProvider;

    public GetUploadFileStorageLocationCommandHandler(
        ITransferLocationService transferLocationService,
        IOptions<AppSettings> appSettings,
        ILogger<GetUploadFileStorageLocationCommandHandler> logger)
    {
        _transferLocationService = transferLocationService;
        _storageProvider = appSettings.Value.DefaultStorageProvider;
        _logger = logger;
    }
    
    public async Task<FileStorageLocation> Handle(GetUploadFileStorageLocationRequest request, CancellationToken cancellationToken)
    {
        var url = await _transferLocationService.GetUploadFileUrl(
            request.SessionId,
            request.Client,
            request.TransferParameters.SharedFileDefinition,
            request.TransferParameters.PartNumber!.Value
        );
        
        var responseObject = new FileStorageLocation(url, _storageProvider);
        
        _logger.LogDebug("Upload file storage location generated for session {SessionId}, file {FileId}", 
            request.SessionId, request.TransferParameters.SharedFileDefinition.Id);
        
        return responseObject;
    }
} 