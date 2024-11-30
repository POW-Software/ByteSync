using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ByteSync.ServerCommon.Interfaces.Storage;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ByteSync.Functions.Timer;

public class CleanupBlobFilesFunction
{
    private readonly ILogger<CleanupBlobFilesFunction> _logger;
    private readonly IConfigurationSection _configurationSection;
    private readonly IBlobContainerProvider _blobContainerProvider;

    public CleanupBlobFilesFunction(IConfiguration configuration, IBlobContainerProvider blobContainerProvider, 
        ILogger<CleanupBlobFilesFunction> logger)
    {
        _configurationSection = configuration.GetSection("BlobStorage");
        _blobContainerProvider = blobContainerProvider;
        _logger = logger;
    }
    
    [Function("CleanupBlobFilesFunction")]
    public async Task<int> RunAsync([TimerTrigger("0 0 0 * * *" 
#if DEBUG
        , RunOnStartup= true
#endif
        )] TimerInfo myTimer)
    {
        _logger.LogInformation("Cleanup function executed at: {Now}", DateTime.Now);
        
        int retentionDurationInDays = _configurationSection.GetValue<int>("RetentionDurationInDays");
        
        if (retentionDurationInDays < 1)
        {
            _logger.LogWarning("RetentionDurationInDays is less than 1, no element deleted");
            return 0;
        }

        BlobContainerClient container = _blobContainerProvider.GetBlobContainerClient();
        if (!await container.ExistsAsync())
        {
            _logger.LogWarning("...Container not found, no element deleted");
            return 0;
        }
        
        int deletedBlobsCount = 0;
        var blobs = container.GetBlobsAsync();
        await foreach (var blobItem in blobs)
        {
            if (blobItem.Properties.CreatedOn != null && blobItem.Properties.CreatedOn <= DateTimeOffset.UtcNow.AddDays(-3))
            {
                _logger.LogInformation("Deleting Obsolete blob {BlobName} (CreatedOn:{CreatedOn})", blobItem.Name, blobItem.Properties.CreatedOn);
                await container.DeleteBlobAsync(blobItem.Name, DeleteSnapshotsOption.IncludeSnapshots);
                
                deletedBlobsCount += 1;
            }
        }
        _logger.LogInformation("...Deletion complete, {Deleted} element(s)", deletedBlobsCount);
        
        return deletedBlobsCount;
    }
}