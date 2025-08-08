using Azure.Storage;
using Azure.Storage.Blobs;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services.Storage;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services.Storage;

public class AzureBlobStorageContainerService : IAzureBlobStorageContainerService
{
    private readonly AzureBlobStorageSettings _blobStorageSettings;
    
    private StorageSharedKeyCredential? _storageSharedKeyCredential;
    private BlobContainerClient? _containerClient;

    public AzureBlobStorageContainerService(IOptions<AzureBlobStorageSettings> blobStorageSettings)
    {
        _blobStorageSettings = blobStorageSettings.Value;
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