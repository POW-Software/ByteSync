using Azure.Storage;
using Azure.Storage.Blobs;
using ByteSync.ServerCommon.Business.Settings;
using ByteSync.ServerCommon.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace ByteSync.ServerCommon.Services;

public class BlobStorageContainerService : IBlobStorageContainerService
{
    private readonly BlobStorageSettings _blobStorageSettings;
    
    private StorageSharedKeyCredential? _storageSharedKeyCredential;

    public BlobStorageContainerService(IOptions<BlobStorageSettings> blobStorageSettings)
    {
        _blobStorageSettings = blobStorageSettings.Value;
    }
    
    public async Task<BlobContainerClient> BuildBlobContainerClient()
    {
        Uri containerUri = BuildContainerUri();
        
        var container = new BlobContainerClient(containerUri, StorageSharedKeyCredential);
        
        await container.CreateIfNotExistsAsync();

        return container;
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