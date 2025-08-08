using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ByteSync.Common.Business.SharedFiles;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ByteSync.ServerCommon.Business.Settings;

namespace ByteSync.ServerCommon.Services.Storage;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly AzureBlobStorageSettings _blobStorageSettings;
    private readonly ILogger<AzureBlobStorageService> _logger;
    
    private StorageSharedKeyCredential? _storageSharedKeyCredential;
    private BlobContainerClient? _containerClient;

    public AzureBlobStorageService(IOptions<AzureBlobStorageSettings> blobStorageSettings, ILogger<AzureBlobStorageService> logger)
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
        var container = await BuildBlobContainerClient();

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
        var container = await BuildBlobContainerClient();

        string finalFileName = GetServerFileName(sharedFileDefinition, partNumber);
            
        BlobClient blobClient = container.GetBlobClient(finalFileName);
            
        _logger.LogInformation("Deleting blob {FileName}", finalFileName);

        var result = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        if (!result.Value)
        {
            _logger.LogWarning("Blob {FileName} not found", finalFileName);
        }
    }

    public async Task<long?> GetObjectSize(SharedFileDefinition sharedFileDefinition, int partNumber)
    {
        var container = await BuildBlobContainerClient();
            
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

    public async Task<BlobContainerClient> BuildBlobContainerClient()
    {
        if (_containerClient == null)
        {
            Uri containerUri = BuildContainerUri();
            _containerClient = new BlobContainerClient(containerUri, StorageSharedKeyCredential);
            await _containerClient.CreateIfNotExistsAsync();
        }

        return _containerClient;
    }

    public StorageSharedKeyCredential StorageSharedKeyCredential
    {
        get
        {
            if (_storageSharedKeyCredential == null)
            {
                _storageSharedKeyCredential = BuildStorageSharedKeyCredential();
            }
            
            return _storageSharedKeyCredential;
        }
    }

    private Uri BuildContainerUri()
    {
        string endpoint = _blobStorageSettings.Endpoint.TrimEnd('/');
        string container = _blobStorageSettings.Container.TrimStart('/').TrimEnd('/') + "/";

        Uri baseUri = new Uri(endpoint);
        Uri fullUri = new Uri(baseUri, container);

        return fullUri;
    }
    
    private StorageSharedKeyCredential BuildStorageSharedKeyCredential()
    {
        return new StorageSharedKeyCredential(_blobStorageSettings.AccountName, _blobStorageSettings.AccountKey);
    }
}