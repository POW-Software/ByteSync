using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using ByteSync.ServerCommon.Interfaces.Services.Storage.Factories;
using Microsoft.Extensions.Logging;

namespace ByteSync.ServerCommon.Services.Storage;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly IAzureBlobContainerClientFactory _clientFactory;

    public AzureBlobStorageService(IAzureBlobContainerClientFactory clientFactory,
        ILogger<AzureBlobStorageService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<string> GetUploadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        return await ComputeUrl(sharedFileDefinition, partNumber, BlobSasPermissions.Write);
    }

    public async Task<string> GetDownloadFileUrl(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        return await ComputeUrl(sharedFileDefinition, partNumber, BlobSasPermissions.Read);
    }

    private async Task<string> ComputeUrl(SharedFileDefinition sharedFileDefinition, int partNumber, BlobSasPermissions permission)
    {
        var container = await _clientFactory.GetOrCreateContainer(CancellationToken.None);

        string finalFileName = GetServerFileName(sharedFileDefinition, partNumber);

        BlobClient blobClient = container.GetBlobClient(finalFileName);

        double minutes;
        if (permission == BlobSasPermissions.Write)
        {
            minutes = 60;
        }
        else
        {
            minutes = 20;
        }
            
        BlobSasBuilder sasBuilder = new BlobSasBuilder()
        {
            BlobContainerName = blobClient.BlobContainerName,
            BlobName = blobClient.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(minutes)
        };

        // Specify read and write permissions for the SAS.
        sasBuilder.SetPermissions(permission);

        // Add the SAS token to the blob URI.
        BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
        {
            // Specify the user delegation key.
            Sas = sasBuilder.ToSasQueryParameters(StorageSharedKeyCredential)
        };

        return blobUriBuilder.ToUri().ToString();
    }

    private string GetServerFileName(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        return sharedFileDefinition.SessionId + "_" + sharedFileDefinition.ClientInstanceId + "_" +
               sharedFileDefinition.GetFileName(partNumber);
    }

    public async Task DeleteObject(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var container = await _clientFactory.GetOrCreateContainer(CancellationToken.None);

        string finalFileName = GetServerFileName(sharedFileDefinition, partNumber);
            
        BlobClient blobClient = container.GetBlobClient(finalFileName);
            
        _logger.LogInformation("Deleting blob {FileName}", finalFileName);

        var result = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        if (!result.Value)
        {
            _logger.LogWarning("Blob {FileName} not found", finalFileName);
        }
    }
    
    private StorageSharedKeyCredential StorageSharedKeyCredential => _clientFactory.GetCredential();

    public async Task<IReadOnlyCollection<KeyValuePair<string, DateTimeOffset?>>> GetAllObjects(CancellationToken cancellationToken)
    {
        var container = await _clientFactory.GetOrCreateContainer(cancellationToken);
        var results = new List<KeyValuePair<string, DateTimeOffset?>>();

        await foreach (var blobItem in container.GetBlobsAsync().WithCancellation(cancellationToken))
        {
            results.Add(new KeyValuePair<string, DateTimeOffset?>(blobItem.Name, blobItem.Properties.CreatedOn));
        }

        return results;
    }

    public async Task DeleteObjectByKey(string key, CancellationToken cancellationToken)
    {
        var container = await _clientFactory.GetOrCreateContainer(cancellationToken);
        await container.DeleteBlobIfExistsAsync(key, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
    }
}