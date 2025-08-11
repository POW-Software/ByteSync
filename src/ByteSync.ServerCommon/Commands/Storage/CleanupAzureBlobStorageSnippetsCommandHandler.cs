using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Commands.Storage;

public class CleanupAzureBlobStorageSnippetsCommandHandler : IRequestHandler<CleanupAzureBlobStorageSnippetsRequest, int>
{
    private readonly IAzureBlobStorageService _azureBlobStorageService;
    private readonly ILogger<CleanupAzureBlobStorageSnippetsCommandHandler> _logger;
    private readonly AzureBlobStorageSettings _blobStorageSettings;

    public CleanupAzureBlobStorageSnippetsCommandHandler(
        IAzureBlobStorageService azureBlobStorageService,
        IOptions<AzureBlobStorageSettings> blobStorageSettings,
        ILogger<CleanupAzureBlobStorageSnippetsCommandHandler> logger)
    {
        _azureBlobStorageService = azureBlobStorageService;
        _blobStorageSettings = blobStorageSettings.Value;
        _logger = logger;
    }

    public async Task<int> Handle(CleanupAzureBlobStorageSnippetsRequest request, CancellationToken cancellationToken)
    {
        if (_blobStorageSettings.RetentionDurationInDays < 1)
        {
            _logger.LogWarning("RetentionDurationInDays is less than 1, no element deleted");
            return 0;
        }

        var deletedBlobsCount = 0;
        var allObjects = await _azureBlobStorageService.GetAllObjects(cancellationToken);

        foreach (var obj in allObjects)
        {
            if (obj.Value != null && obj.Value <= DateTimeOffset.UtcNow.AddDays(-_blobStorageSettings.RetentionDurationInDays))
            {
                _logger.LogInformation("Deleting obsolete blob {Key} (LastModified:{LastModified})", obj.Key, obj.Value);
                await _azureBlobStorageService.DeleteObjectByKey(obj.Key, cancellationToken);
                deletedBlobsCount += 1;
            }
        }

        return deletedBlobsCount;
    }
} 