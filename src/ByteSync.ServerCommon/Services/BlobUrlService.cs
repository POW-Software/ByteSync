using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class BlobUrlService : IBlobUrlService
{
    private readonly BlobStorageSettings _blobStorageSettings;
    private readonly ILogger<BlobUrlService> _logger;

    public BlobUrlService(IOptions<BlobStorageSettings> blobStorageSettings, ILogger<BlobUrlService> logger)
    {
        _blobStorageSettings = blobStorageSettings.Value;
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
        var storageSharedKeyCredential = BuildStorageSharedKeyCredential();
        var container = await BuildBlobContainerClient(storageSharedKeyCredential);

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
            Sas = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential)
        };

        return blobUriBuilder.ToUri().ToString();
    }
    private StorageSharedKeyCredential BuildStorageSharedKeyCredential()
    {
        return new StorageSharedKeyCredential(_blobStorageSettings.AccountName, _blobStorageSettings.AccountKey);
    }

    private async Task<BlobContainerClient> BuildBlobContainerClient(StorageSharedKeyCredential storageSharedKeyCredential)
    {
        Uri containerUri = BuildContainerUri();
        
        var container = new BlobContainerClient(containerUri, storageSharedKeyCredential);
        
        await container.CreateIfNotExistsAsync();

        return container;
    }

    private Uri BuildContainerUri()
    {
        string endpoint = _blobStorageSettings.Endpoint.TrimEnd('/');
        string container = _blobStorageSettings.Container.TrimStart('/').TrimEnd('/') + "/";

        Uri baseUri = new Uri(endpoint);
        Uri fullUri = new Uri(baseUri, container);

        return fullUri;
    }

    private string GetServerFileName(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        return sharedFileDefinition.SessionId + "_" + sharedFileDefinition.ClientInstanceId + "_" +
               sharedFileDefinition.GetFileName(partNumber);
    }

    public async Task DeleteBlob(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var storageSharedKeyCredential = BuildStorageSharedKeyCredential();
        var container = await BuildBlobContainerClient(storageSharedKeyCredential);

        string finalFileName = GetServerFileName(sharedFileDefinition, partNumber);
            
        BlobClient blobClient = container.GetBlobClient(finalFileName);
            
        _logger.LogInformation("Deleting blob {FileName}", finalFileName);

        var result = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        if (!result.Value)
        {
            _logger.LogWarning("Blob {FileName} not found", finalFileName);
        }
    }

    public async Task<long?> GetBlobSize(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var storageSharedKeyCredential = BuildStorageSharedKeyCredential();
        var container = await BuildBlobContainerClient(storageSharedKeyCredential);
            
        string finalFileName = GetServerFileName(sharedFileDefinition, partNumber);
            
        BlobClient blobClient = container.GetBlobClient(finalFileName);

        long? result = null;
        if (blobClient != null)
        {
            var response =  await blobClient.GetPropertiesAsync();
            result = response.Value.ContentLength;
        }

        return result;
    }
}