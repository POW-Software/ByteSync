using Azure.Storage.Blobs.Models;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Commands.Storage;

public class CleanupBlobStorageSnippetsCommandHandler : IRequestHandler<CleanupBlobStorageSnippetsRequest, int>
{
    private readonly IBlobStorageContainerService _blobStorageContainerService;
    private readonly ILogger<CleanupBlobStorageSnippetsCommandHandler> _logger;
    private readonly BlobStorageSettings _blobStorageSettings;

    public CleanupBlobStorageSnippetsCommandHandler(
        IBlobStorageContainerService blobStorageContainerService,
        IOptions<BlobStorageSettings> blobStorageSettings,
        ILogger<CleanupBlobStorageSnippetsCommandHandler> logger)
    {
        _blobStorageContainerService = blobStorageContainerService;
        _blobStorageSettings = blobStorageSettings.Value;
        _logger = logger;
    }

    public async Task<int> Handle(CleanupBlobStorageSnippetsRequest request, CancellationToken cancellationToken)
    {
        if (_blobStorageSettings.RetentionDurationInDays < 1)
        {
            _logger.LogWarning("RetentionDurationInDays is less than 1, no element deleted");
            return 0;
        }

        var container = await _blobStorageContainerService.BuildBlobContainerClient();
        if (!await container.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning("...Container not found, no element deleted");
            return 0;
        }

        var deletedBlobsCount = 0;
        var blobs = container.GetBlobsAsync();
        await foreach (var blobItem in blobs.WithCancellation(cancellationToken))
        {
            if (blobItem.Properties.CreatedOn != null &&
                blobItem.Properties.CreatedOn <= DateTimeOffset.UtcNow.AddDays(-_blobStorageSettings.RetentionDurationInDays))
            {
                _logger.LogInformation("Deleting Obsolete blob {BlobName} (CreatedOn:{CreatedOn})", blobItem.Name, blobItem.Properties.CreatedOn);
                await container.DeleteBlobAsync(blobItem.Name, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
                deletedBlobsCount += 1;
            }
        }

        return deletedBlobsCount;
    }
} 